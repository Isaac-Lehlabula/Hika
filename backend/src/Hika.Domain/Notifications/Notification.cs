using Hika.Domain.Common;

namespace Hika.Domain.Notifications;

public enum NotificationType
{
    BookingRequested,
    BookingAccepted,
    BookingDeclined,
    TripCancelled,
    PaymentSucceeded,
    NewReview,
    RideAlertMatched,
}

/// <summary>InApp is the only channel actually delivered by this phase — see
/// NotificationDispatcher. Email/Sms/Push exist on the enum because
/// docs/domain-model.md §8 designs for them, but wiring real delivery through
/// IEmailSender/ISmsSender (and Push, which additionally needs device-token registration and
/// real FCM credentials neither of which exist in this environment) is a follow-up.</summary>
public enum NotificationChannel
{
    InApp,
    Email,
    Sms,
    Push,
}

public enum NotificationStatus
{
    Sent,
    Read,
}

/// <summary>
/// Every notification is persisted regardless of channel — the in-app inbox is just "my
/// Notification rows" (see docs/domain-model.md §8). Message is a pre-rendered, human-readable
/// string rather than a structured jsonb payload the client re-templates — a deliberate MVP
/// simplification that avoids needing per-NotificationType client-side rendering logic.
/// </summary>
public sealed class Notification : AuditableEntity
{
    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public string Message { get; private set; }

    /// <summary>The booking/trip/review this notification is about, for deep-linking. Not a
    /// foreign key to any one table since the related entity type varies by NotificationType.</summary>
    public Guid? RelatedEntityId { get; private set; }

    public NotificationStatus Status { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    private Notification()
    {
        Message = string.Empty;
    }

    public static Notification Create(Guid userId, NotificationType type, string message, Guid? relatedEntityId) => new()
    {
        UserId = userId,
        Type = type,
        Channel = NotificationChannel.InApp,
        Message = message,
        RelatedEntityId = relatedEntityId,
        Status = NotificationStatus.Sent,
    };

    public void MarkRead()
    {
        if (Status == NotificationStatus.Read)
        {
            return;
        }

        Status = NotificationStatus.Read;
        ReadAtUtc = DateTimeOffset.UtcNow;
    }
}
