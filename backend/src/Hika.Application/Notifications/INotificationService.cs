using Hika.Application.Common.Pagination;
using Hika.Application.Notifications.Dtos;

namespace Hika.Application.Notifications;

public interface INotificationService
{
    Task<PagedResult<NotificationResponse>> GetMyNotificationsAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);
}
