using Hika.Domain.Common;

namespace Hika.Domain.Bookings;

public enum BookingStatus
{
    Pending,

    /// <summary>Driver has accepted; the passenger has been sent to complete a redirect-based
    /// payment (Ozow) and Ozow hasn't confirmed it yet. Skipped entirely for gateways that
    /// settle synchronously (MockPaymentGateway) — those go straight from Pending to Confirmed,
    /// same as before this status existed. See PaymentService.CompletePaymentAsync.</summary>
    AwaitingPayment,

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

    /// <summary>Always lands on AwaitingPayment, never directly on Confirmed — whether that
    /// resolves in the same request (a synchronously-settling gateway) or minutes later (a
    /// redirect-based one) is an application-layer concern this entity doesn't need to know
    /// about. See ConfirmPayment/FailPayment.</summary>
    public void Accept()
    {
        if (Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot accept a booking that is {Status}.");
        }

        Status = BookingStatus.AwaitingPayment;
        RespondedAtUtc = DateTimeOffset.UtcNow;
    }

    public void ConfirmPayment()
    {
        if (Status != BookingStatus.AwaitingPayment)
        {
            throw new InvalidOperationException($"Cannot confirm payment for a booking that is {Status}.");
        }

        Status = BookingStatus.Confirmed;
    }

    /// <summary>Lands on Declined, not a dedicated status — from the passenger's perspective
    /// "the driver accepted but payment didn't go through" and "the driver declined" both mean
    /// the same thing: this booking isn't happening, seats are released. See
    /// BookingService.CompletePaymentAsync for the seat-release that accompanies this.</summary>
    public void FailPayment()
    {
        if (Status != BookingStatus.AwaitingPayment)
        {
            throw new InvalidOperationException($"Cannot fail payment for a booking that is {Status}.");
        }

        Status = BookingStatus.Declined;
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
        if (Status is not (BookingStatus.Pending or BookingStatus.AwaitingPayment or BookingStatus.Confirmed))
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
