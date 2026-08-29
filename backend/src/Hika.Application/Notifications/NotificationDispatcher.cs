using Hika.Application.Common.Persistence;
using Hika.Application.Notifications.Ports;
using Hika.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hika.Application.Notifications;

public sealed class NotificationDispatcher(IAppDbContext db, IPushSender pushSender, ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    public async Task DispatchAsync(Guid userId, NotificationType type, string message, Guid? relatedEntityId, CancellationToken cancellationToken)
    {
        db.Notifications.Add(Notification.Create(userId, type, message, relatedEntityId));

        // Best-effort, and deliberately not awaited into the caller's own SaveChangesAsync unit
        // of work the way the InApp row above is — a push failure (no device tokens yet, FCM
        // unreachable, an invalid/stale token) must never take down the request that triggered
        // it. The InApp row is the channel this app actually guarantees; push is additive.
        try
        {
            var deviceTokens = await db.DeviceTokens
                .Where(t => t.UserId == userId)
                .Select(t => t.Token)
                .ToListAsync(cancellationToken);

            if (deviceTokens.Count > 0)
            {
                await pushSender.SendAsync(deviceTokens, "Hiking Spot", message, BuildData(type, relatedEntityId), cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to push-notify user {UserId} for {NotificationType}", userId, type);
        }
    }

    private static Dictionary<string, string>? BuildData(NotificationType type, Guid? relatedEntityId)
    {
        if (relatedEntityId is null)
        {
            return null;
        }

        return new Dictionary<string, string> { ["type"] = type.ToString(), ["relatedEntityId"] = relatedEntityId.Value.ToString() };
    }
}
