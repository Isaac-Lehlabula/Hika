using Hika.Application.Common.Persistence;
using Hika.Domain.Notifications;

namespace Hika.Application.Notifications;

public sealed class NotificationDispatcher(IAppDbContext db) : INotificationDispatcher
{
    public Task DispatchAsync(Guid userId, NotificationType type, string message, Guid? relatedEntityId, CancellationToken cancellationToken)
    {
        db.Notifications.Add(Notification.Create(userId, type, message, relatedEntityId));
        return Task.CompletedTask;
    }
}
