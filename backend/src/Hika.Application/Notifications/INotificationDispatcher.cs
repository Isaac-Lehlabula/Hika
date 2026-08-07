using Hika.Domain.Notifications;

namespace Hika.Application.Notifications;

/// <summary>
/// The fan-out point other application services call to notify a user of something (a booking
/// request, an accepted booking, a new review, ...) without needing to know how notifications
/// are delivered. Adds the Notification to the current unit of work without saving — callers
/// (BookingService, TripService, ReviewService) save once, atomically, alongside whatever else
/// they were already persisting, same pattern as IPaymentService.CapturePaymentAsync.
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(Guid userId, NotificationType type, string message, Guid? relatedEntityId, CancellationToken cancellationToken);
}
