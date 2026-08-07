using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Notifications;
using Hika.Application.Trips.Dtos;
using Hika.Domain.Common;
using Hika.Domain.Drivers;
using Hika.Domain.Notifications;
using Hika.Domain.RideAlerts;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Trips;

public sealed class TripService(IAppDbContext db, INotificationDispatcher notificationDispatcher) : ITripService
{
    public async Task<TripResponse> CreateAsync(Guid driverUserId, CreateTripRequest request, CancellationToken cancellationToken)
    {
        var driverExists = await db.DriverProfiles.AnyAsync(p => p.Id == driverUserId, cancellationToken);
        if (!driverExists)
        {
            throw new AppValidationException("driverProfile", "Create a driver profile before posting a trip.");
        }

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == request.VehicleId, cancellationToken);
        if (vehicle is null || vehicle.DriverProfileId != driverUserId)
        {
            throw new AppValidationException("vehicleId", "Select one of your own vehicles.");
        }

        if (request.TotalSeatsOffered > vehicle.SeatCapacity)
        {
            throw new AppValidationException(
                "totalSeatsOffered", $"This vehicle can offer at most {vehicle.SeatCapacity} seats.");
        }

        var stopInputs = request.Stops
            .Select(s => new TripStopInput(s.LocationId, s.RawName, s.Province))
            .ToList();

        var trip = Trip.Create(
            driverUserId,
            request.VehicleId,
            request.DepartureAtUtc.ToUniversalTime(),
            request.TotalSeatsOffered,
            new Money(request.PricePerSeat),
            request.LuggageAllowance,
            request.Notes,
            stopInputs);

        db.Trips.Add(trip);
        await MatchRideAlertsAsync(trip, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(trip.Id, cancellationToken)
            ?? throw new InvalidOperationException("Trip was created but could not be re-read.");
    }

    /// <summary>"Notify me when someone posts JHB → Giyani" — matched the same way search
    /// matches a trip's stops (RawName substring, ordered), see docs/domain-model.md §8. Fires
    /// once per alert then marks it Fulfilled; a rider who wants to keep watching creates a
    /// new one (see RideAlert.MarkFulfilled).</summary>
    private async Task MatchRideAlertsAsync(Trip trip, CancellationToken cancellationToken)
    {
        var activeAlerts = await db.RideAlerts.Where(a => a.Status == RideAlertStatus.Active).ToListAsync(cancellationToken);
        if (activeAlerts.Count == 0)
        {
            return;
        }

        // South Africa runs UTC+2 year-round (no DST) — see docs/south-africa.md.
        var tripDepartureDate = DateOnly.FromDateTime(trip.DepartureAtUtc.ToOffset(TimeSpan.FromHours(2)).DateTime);

        foreach (var alert in activeAlerts)
        {
            var origin = alert.OriginRawText.ToLowerInvariant();
            var destination = alert.DestinationRawText.ToLowerInvariant();

            var originStop = trip.Stops.FirstOrDefault(s => s.RawName.ToLowerInvariant().Contains(origin));
            var destinationStop = trip.Stops.FirstOrDefault(s => s.RawName.ToLowerInvariant().Contains(destination));

            if (originStop is null || destinationStop is null || originStop.Sequence >= destinationStop.Sequence)
            {
                continue;
            }

            if (alert.TravelDate is { } travelDate && travelDate != tripDepartureDate)
            {
                continue;
            }

            alert.MarkFulfilled();
            await notificationDispatcher.DispatchAsync(
                alert.UserId,
                NotificationType.RideAlertMatched,
                $"A trip matching your alert ({alert.OriginRawText} → {alert.DestinationRawText}) was just posted!",
                trip.Id,
                cancellationToken);
        }
    }

    public async Task<TripResponse> GetAsync(Guid tripId, CancellationToken cancellationToken) =>
        await BuildResponseAsync(tripId, cancellationToken) ?? throw new NotFoundException(nameof(Trip), tripId);

    public async Task<IReadOnlyList<TripSummaryResponse>> GetMyTripsAsync(Guid driverUserId, CancellationToken cancellationToken)
    {
        var trips = await db.Trips
            .Include(t => t.Stops)
            .Include(t => t.Segments)
            .Where(t => t.DriverProfileId == driverUserId)
            .OrderByDescending(t => t.DepartureAtUtc)
            .ToListAsync(cancellationToken);

        if (trips.Count == 0)
        {
            return [];
        }

        var driverSummary = (await TripDisplayHelpers.BuildDriverSummariesAsync(db, [driverUserId], cancellationToken))
            .GetValueOrDefault(driverUserId)
            ?? throw new InvalidOperationException("Driver profile disappeared mid-query.");

        var locations = await TripDisplayHelpers.LoadLocationsAsync(db, trips.SelectMany(t => t.Stops), cancellationToken);

        return trips.Select(t => ToSummary(t, driverSummary, locations)).ToList();
    }

    public async Task CancelAsync(Guid driverUserId, Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken)
            ?? throw new NotFoundException(nameof(Trip), tripId);

        if (trip.DriverProfileId != driverUserId)
        {
            throw new NotFoundException(nameof(Trip), tripId);
        }

        // Deliberately not yet cascading to Bookings/Payments here: declining pending bookings,
        // releasing their held seats, and refunding confirmed ones on trip cancellation is a
        // real gap (tracked in docs/roadmap.md), but the policy questions it raises (does the
        // driver owe a refund? partial vs. full?) belong with Trust & Safety (Phase 10) or
        // hardening, not bolted on here without that design.
        trip.Cancel();
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TripResponse?> BuildResponseAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await db.Trips
            .Include(t => t.Stops)
            .Include(t => t.Segments)
            .FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        var driverSummary = (await TripDisplayHelpers.BuildDriverSummariesAsync(db, [trip.DriverProfileId], cancellationToken))
            .GetValueOrDefault(trip.DriverProfileId)
            ?? throw new InvalidOperationException("Trip references a driver profile that no longer exists.");

        var vehicle = await db.Vehicles.Include(v => v.Photos).FirstOrDefaultAsync(v => v.Id == trip.VehicleId, cancellationToken)
            ?? throw new InvalidOperationException("Trip references a vehicle that no longer exists.");

        var locations = await TripDisplayHelpers.LoadLocationsAsync(db, trip.Stops, cancellationToken);
        var stopsBySequence = trip.Stops.ToDictionary(s => s.Id, s => s.Sequence);

        return new TripResponse
        {
            Id = trip.Id,
            Status = trip.Status.ToString(),
            DepartureAtUtc = trip.DepartureAtUtc,
            TotalSeatsOffered = trip.TotalSeatsOffered,
            PricePerSeat = trip.PricePerSeat.Amount,
            LuggageAllowance = trip.LuggageAllowance,
            Notes = trip.Notes,
            Driver = driverSummary,
            Vehicle = TripDisplayHelpers.ToVehicleSummary(vehicle),
            Stops = trip.Stops.Select(s => ToStopResponse(s, locations)).ToList(),
            Segments = trip.Segments
                .Select(seg => new TripSegmentResponse
                {
                    FromSequence = stopsBySequence[seg.FromStopId],
                    ToSequence = stopsBySequence[seg.ToStopId],
                    SeatsAvailable = seg.SeatsAvailable,
                })
                .OrderBy(s => s.FromSequence)
                .ToList(),
        };
    }

    private static TripStopResponse ToStopResponse(TripStop stop, IReadOnlyDictionary<Guid, Location> locations) => new()
    {
        Sequence = stop.Sequence,
        LocationId = stop.LocationId,
        Name = TripDisplayHelpers.ResolveStopName(stop, locations),
        Province = stop.Province.ToString(),
    };

    private static TripSummaryResponse ToSummary(Trip trip, TripDriverSummary driver, IReadOnlyDictionary<Guid, Location> locations)
    {
        var stops = trip.Stops;
        var origin = ToStopResponse(stops[0], locations);
        var destination = ToStopResponse(stops[^1], locations);

        return new TripSummaryResponse
        {
            Id = trip.Id,
            Status = trip.Status.ToString(),
            DepartureAtUtc = trip.DepartureAtUtc,
            OriginName = origin.Name,
            DestinationName = destination.Name,
            TotalSeatsOffered = trip.TotalSeatsOffered,
            MinSeatsAvailable = trip.Segments.Count == 0 ? 0 : trip.Segments.Min(s => s.SeatsAvailable),
            PricePerSeat = trip.PricePerSeat.Amount,
            Driver = driver,
        };
    }
}
