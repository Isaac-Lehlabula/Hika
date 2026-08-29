using Hika.Domain.Common;

namespace Hika.Domain.RideRequests;

public enum RideRequestStatus
{
    Open,
    Claimed,
    Cancelled,
}

/// <summary>
/// "I need a lift JHB → Giyani on 20 Dec, 2 seats" — posted by a rider when nothing matching is
/// on offer yet, visible to drivers (unlike RideAlert, which only ever notifies the rider who
/// created it — see RideRequestService's remarks on the distinction). A driver fulfils one by
/// claiming it against one of their own trips, which is the accept-equivalent for this path: see
/// RideRequestService.ClaimAsync, which drives the same Booking.Request→Accept sequence a normal
/// search-and-request does, so payment, chat, reviews, and payout all just work unmodified.
///
/// No background sweep marks a request Expired — TravelDate is filtered directly wherever "open"
/// requests are queried (see RideRequestService), since nothing in this codebase runs scheduled
/// jobs yet. IsExpired is computed for display, not persisted as a status.
/// </summary>
public sealed class RideRequest : AuditableEntity
{
    public Guid RiderUserId { get; private set; }

    public string OriginRawText { get; private set; }

    public string DestinationRawText { get; private set; }

    public DateOnly TravelDate { get; private set; }

    public int SeatsNeeded { get; private set; }

    /// <summary>Advisory only — shown to drivers browsing open requests, but the booking a claim
    /// produces is still charged at the claiming trip's own PricePerSeat (unchanged existing
    /// behavior). Turning this into a binding counter-offer a driver must explicitly accept is a
    /// larger negotiation feature, deliberately not built here — see the remarks on
    /// RideRequestService.ClaimAsync.</summary>
    public decimal? ProposedPricePerSeat { get; private set; }

    public RideRequestStatus Status { get; private set; }

    public Guid? ClaimedByDriverUserId { get; private set; }

    public Guid? ClaimedBookingId { get; private set; }

    private RideRequest()
    {
        OriginRawText = string.Empty;
        DestinationRawText = string.Empty;
    }

    public static RideRequest Post(
        Guid riderUserId, string originRawText, string destinationRawText, DateOnly travelDate, int seatsNeeded, decimal? proposedPricePerSeat)
    {
        if (seatsNeeded < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seatsNeeded), seatsNeeded, "Must request at least one seat.");
        }

        if (proposedPricePerSeat is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(proposedPricePerSeat), proposedPricePerSeat, "Proposed price can't be negative.");
        }

        return new RideRequest
        {
            RiderUserId = riderUserId,
            OriginRawText = originRawText,
            DestinationRawText = destinationRawText,
            TravelDate = travelDate,
            SeatsNeeded = seatsNeeded,
            ProposedPricePerSeat = proposedPricePerSeat,
            Status = RideRequestStatus.Open,
        };
    }

    public void Claim(Guid driverUserId, Guid bookingId)
    {
        if (Status != RideRequestStatus.Open)
        {
            throw new InvalidOperationException($"Cannot claim a {Status} ride request.");
        }

        Status = RideRequestStatus.Claimed;
        ClaimedByDriverUserId = driverUserId;
        ClaimedBookingId = bookingId;
    }

    public void Cancel()
    {
        if (Status != RideRequestStatus.Open)
        {
            throw new InvalidOperationException($"Cannot cancel a {Status} ride request.");
        }

        Status = RideRequestStatus.Cancelled;
    }
}
