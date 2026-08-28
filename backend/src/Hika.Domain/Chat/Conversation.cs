using Hika.Domain.Common;

namespace Hika.Domain.Chat;

/// <summary>
/// A 1:1 thread between a trip's driver and a passenger, scoped to exactly one Booking. Opens
/// when the driver accepts (or claims) that booking, closes when the booking reaches a terminal
/// state (completed, or fell through after acceptance) — see BookingService's Accept/Complete/
/// CancelAsync and ResolvePaymentOutcomeAsync, which call OpenForBookingAsync/CloseForBookingAsync
/// on IChatService at exactly those transitions. Never opens for a Pending or Declined booking —
/// there's nothing to coordinate before a driver has actually committed to the trip.
/// </summary>
public sealed class Conversation : AuditableEntity
{
    public Guid BookingId { get; private set; }

    public DateTimeOffset OpenedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public bool IsOpen => ClosedAtUtc is null;

    private Conversation()
    {
    }

    public static Conversation Open(Guid bookingId) => new()
    {
        BookingId = bookingId,
        OpenedAtUtc = DateTimeOffset.UtcNow,
    };

    /// <summary>Idempotent — BookingService calls this from several different terminal
    /// transitions (Complete, a failed payment, a late cancellation) and shouldn't need to know
    /// whether it already closed.</summary>
    public void Close()
    {
        ClosedAtUtc ??= DateTimeOffset.UtcNow;
    }
}
