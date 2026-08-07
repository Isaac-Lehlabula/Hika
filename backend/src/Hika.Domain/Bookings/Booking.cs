using Hika.Domain.Common;

namespace Hika.Domain.Bookings;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Declined,
    Cancelled,
    Completed,
}

/// <summary>
/// A passenger's reservation for a contiguous sub-range of a Trip's stops. Seats are reserved
/// (TripSegment.SeatsAvailable decremented) the moment a booking is requested, not when the
/// driver accepts — see docs/domain-model.md §4.3 and BookingService.RequestAsync for the
/// concurrency-safe reservation this class doesn't itself perform (it crosses into the Trip
/// aggregate's segments, which is BookingService's job, not this entity's).
/// </summary>
public sealed class Booking : AuditableEntity
{
    public Guid TripId { get; private set; }

    public Guid PassengerUserId { get; private set; }

    public Guid BoardingStopId { get; private set; }

    public Guid AlightingStopId { get; private set; }

    public int SeatsRequested { get; private set; }

    /// <summary>Seats x the trip's price-per-seat at request time — captured, not recomputed
    /// later, so a subsequent price change never retroactively alters a booking.</summary>
    public Money TotalPrice { get; private set; }

    public BookingStatus Status { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; private set; }

    public DateTimeOffset? RespondedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    private readonly List<BookingPassenger> _passengers = [];

    /// <summary>MVP always has exactly one row (the account holder) — named companion
    /// passengers are a natural follow-up the schema already supports.</summary>
    public IReadOnlyCollection<BookingPassenger> Passengers => _passengers.AsReadOnly();

    private Booking()
    {
        TotalPrice = Money.Zero();
    }

    public static Booking Request(
        Guid tripId,
        Guid passengerUserId,
        Guid boardingStopId,
        Guid alightingStopId,
        int seatsRequested,
        Money totalPrice,
        string accountHolderFullName,
        string? accountHolderPhoneNumber)
    {
        if (seatsRequested < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seatsRequested), seatsRequested, "Must request at least one seat.");
        }

        var booking = new Booking
        {
            TripId = tripId,
            PassengerUserId = passengerUserId,
            BoardingStopId = boardingStopId,
            AlightingStopId = alightingStopId,
            SeatsRequested = seatsRequested,
            TotalPrice = totalPrice,
            Status = BookingStatus.Pending,
            RequestedAtUtc = DateTimeOffset.UtcNow,
        };

        booking._passengers.Add(new BookingPassenger(booking.Id, accountHolderFullName, accountHolderPhoneNumber, isAccountHolder: true));

        return booking;
    }

    public void Accept()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot accept a booking that is {Status}.");
        }

        Status = BookingStatus.Confirmed;
        RespondedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Decline()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot decline a booking that is {Status}.");
        }

        Status = BookingStatus.Declined;
        RespondedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason)
    {
        if (Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            throw new InvalidOperationException($"Cannot cancel a booking that is {Status}.");
        }

        Status = BookingStatus.Cancelled;
        CancelledAtUtc = DateTimeOffset.UtcNow;
        CancellationReason = reason;
    }

    public void Complete()
    {
        if (Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException($"Cannot complete a booking that is {Status}.");
        }

        Status = BookingStatus.Completed;
    }
}
