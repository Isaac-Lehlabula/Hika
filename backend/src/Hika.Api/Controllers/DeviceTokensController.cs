using Hika.Api.Common;
using Hika.Application.Notifications;
using Hika.Application.Notifications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/users/me/device-tokens")]
[Authorize]
public sealed class DeviceTokensController(IDeviceTokenService deviceTokenService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register(RegisterDeviceTokenRequest request, CancellationToken cancellationToken)
    {
        await deviceTokenService.RegisterAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Unregister([FromQuery] string token, CancellationToken cancellationToken)
    {
        await deviceTokenService.UnregisterAsync(User.GetUserId(), token, cancellationToken);
        return NoContent();
    }
}
