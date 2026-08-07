using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Trips.Dtos;
using Hika.Domain.Common;
using Hika.Domain.Drivers;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Trips;

public sealed class TripService(IAppDbContext db) : ITripService
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
        await db.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(trip.Id, cancellationToken)
            ?? throw new InvalidOperationException("Trip was created but could not be re-read.");
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

        // Phase 6 will additionally decline pending bookings and trigger refunds for confirmed
        // ones when a trip is cancelled — not needed yet since bookings don't exist until then.
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
