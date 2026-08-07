using Hika.Api.Common;
using Hika.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await notificationService.GetMyNotificationsAsync(User.GetUserId(), page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(User.GetUserId(), notificationId, cancellationToken);
        return NoContent();
    }
}
