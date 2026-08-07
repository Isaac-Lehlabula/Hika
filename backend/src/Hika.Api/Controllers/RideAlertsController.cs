using Hika.Api.Common;
using Hika.Application.RideAlerts;
using Hika.Application.RideAlerts.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/ride-alerts")]
[Authorize]
public sealed class RideAlertsController(IRideAlertService rideAlertService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RideAlertResponse>> CreateAlert(CreateRideAlertRequest request, CancellationToken cancellationToken)
    {
        var alert = await rideAlertService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return Ok(alert);
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<RideAlertResponse>>> GetMyAlerts(CancellationToken cancellationToken)
    {
        var alerts = await rideAlertService.GetMyAlertsAsync(User.GetUserId(), cancellationToken);
        return Ok(alerts);
    }

    [HttpDelete("{alertId:guid}")]
    public async Task<IActionResult> DeleteAlert(Guid alertId, CancellationToken cancellationToken)
    {
        await rideAlertService.DeleteAsync(User.GetUserId(), alertId, cancellationToken);
        return NoContent();
    }
}
