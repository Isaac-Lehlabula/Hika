using Hika.Application.Admin;
using Hika.Application.Admin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/analytics")]
[Authorize(Policy = "Admin")]
public sealed class AdminAnalyticsController(IAdminAnalyticsService adminAnalyticsService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<AnalyticsOverviewResponse>> GetOverview(CancellationToken cancellationToken)
    {
        var overview = await adminAnalyticsService.GetOverviewAsync(cancellationToken);
        return Ok(overview);
    }
}
