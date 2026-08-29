using Hika.Application.Bookings;
using Hika.Application.Bookings.Dtos;
using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.RideRequests.Dtos;
using Hika.Domain.RideRequests;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.RideRequests;

/// <summary>
/// South Africa runs UTC+2 year-round (no DST) — "today" for expiry purposes is computed against
/// that offset, same as TripService.MatchRideAlertsAsync, not raw UTC.
/// </summary>
public sealed class RideRequestService(IAppDbContext db, IBookingService bookingService) : IRideRequestService
{
    public async Task<RideRequestResponse> CreateAsync(Guid riderUserId, CreateRideRequestRequest request, CancellationToken cancellationToken)
    {
        var rideRequest = RideRequest.Post(
            riderUserId, request.Origin.Trim(), request.Destination.Trim(), request.TravelDate, request.SeatsNeeded, request.ProposedPricePerSeat);

        db.RideRequests.Add(rideRequest);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(rideRequest);
    }

    public async Task<IReadOnlyList<RideRequestResponse>> GetMyRequestsAsync(Guid riderUserId, CancellationToken cancellationToken)
    {
        var requests = await db.RideRequests
            .Where(r => r.RiderUserId == riderUserId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return requests.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<RideRequestResponse>> GetOpenRequestsAsync(CancellationToken cancellationToken)
    {
        var today = Today();

        var requests = await db.RideRequests
            .Where(r => r.Status == RideRequestStatus.Open && r.TravelDate >= today)
            .OrderBy(r => r.TravelDate)
            .ToListAsync(cancellationToken);

        return requests.Select(ToResponse).ToList();
    }

    public async Task CancelAsync(Guid riderUserId, Guid requestId, CancellationToken cancellationToken)
    {
        var rideRequest = await db.RideRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException(nameof(RideRequest), requestId);

        if (rideRequest.RiderUserId != riderUserId)
        {
            throw new NotFoundException(nameof(RideRequest), requestId);
        }

        rideRequest.Cancel();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BookingResponse> ClaimAsync(
        Guid driverUserId, Guid requestId, ClaimRideRequestRequest request, CancellationToken cancellationToken)
    {
        var rideRequest = await db.RideRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new NotFoundException(nameof(RideRequest), requestId);

        if (rideRequest.Status != RideRequestStatus.Open)
        {
            throw new ConflictException("This ride request is no longer open.");
        }

        if (rideRequest.TravelDate < Today())
        {
            throw new ConflictException("This ride request has expired.");
        }

        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        if (trip.DriverProfileId != driverUserId)
        {
            throw new NotFoundException(nameof(Trip), request.TripId);
        }

        // The travel date is an objective, unambiguous check worth enforcing — but the
        // origin/destination text isn't: the driver is a human explicitly picking this trip and
        // these exact stops to fulfil the request (unlike RideAlert's fully automated match,
        // there's real judgment in the loop here), and free-text place names vary too much
        // ("Jozi" vs "Johannesburg") to gate a deliberate human choice on a substring match.
        var tripDepartureDate = DateOnly.FromDateTime(trip.DepartureAtUtc.ToOffset(TimeSpan.FromHours(2)).DateTime);
        if (tripDepartureDate != rideRequest.TravelDate)
        {
            throw new AppValidationException("tripId", "This trip doesn't depart on the date the rider requested.");
        }

        // Drives the exact same Request→Accept sequence a normal search-and-request does, so
        // seat reservation, payment, chat, reviews, and payout are all the existing, tested
        // paths — nothing ride-request-specific to duplicate or get subtly wrong.
        var booking = await bookingService.RequestAsync(
            rideRequest.RiderUserId,
            new CreateBookingRequest
            {
                TripId = request.TripId,
                BoardingStopSequence = request.BoardingStopSequence,
                AlightingStopSequence = request.AlightingStopSequence,
                SeatsRequested = rideRequest.SeatsNeeded,
            },
            cancellationToken);
        var accepted = await bookingService.AcceptAsync(driverUserId, booking.Id, cancellationToken);

        rideRequest.Claim(driverUserId, booking.Id);
        await db.SaveChangesAsync(cancellationToken);

        return accepted;
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2)).DateTime);

    private static RideRequestResponse ToResponse(RideRequest rideRequest) => new()
    {
        Id = rideRequest.Id,
        OriginRawText = rideRequest.OriginRawText,
        DestinationRawText = rideRequest.DestinationRawText,
        TravelDate = rideRequest.TravelDate,
        SeatsNeeded = rideRequest.SeatsNeeded,
        ProposedPricePerSeat = rideRequest.ProposedPricePerSeat,
        Status = rideRequest.Status.ToString(),
        IsExpired = rideRequest.Status == RideRequestStatus.Open && rideRequest.TravelDate < Today(),
        ClaimedBookingId = rideRequest.ClaimedBookingId,
        CreatedAtUtc = rideRequest.CreatedAtUtc,
    };
}
