using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Application.Notifications.Dtos;
using Hika.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Notifications;

public sealed class NotificationService(IAppDbContext db) : INotificationService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<NotificationResponse>> GetMyNotificationsAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var notifications = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var responses = notifications.Select(ToResponse).ToList();

        return PagedResult<NotificationResponse>.Create(responses, page, pageSize, totalCount);
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), notificationId);

        if (notification.UserId != userId)
        {
            throw new NotFoundException(nameof(Notification), notificationId);
        }

        notification.MarkRead();
        await db.SaveChangesAsync(cancellationToken);
    }

    private static NotificationResponse ToResponse(Notification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type.ToString(),
        Message = notification.Message,
        RelatedEntityId = notification.RelatedEntityId,
        Status = notification.Status.ToString(),
        CreatedAtUtc = notification.CreatedAtUtc,
        ReadAtUtc = notification.ReadAtUtc,
    };
}
