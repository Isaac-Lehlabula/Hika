using Hika.Api.Common;
using Hika.Api.RateLimiting;
using Hika.Application.TrustSafety;
using Hika.Application.TrustSafety.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/trust-safety")]
[Authorize]
public sealed class TrustSafetyController(IReportService reportService, IBlockService blockService) : ControllerBase
{
    [HttpPost("reports")]
    [EnableRateLimiting(RateLimitingExtensions.ReportsPolicy)]
    public async Task<ActionResult<ReportResponse>> FileReport(CreateReportRequest request, CancellationToken cancellationToken)
    {
        var report = await reportService.FileAsync(User.GetUserId(), request, cancellationToken);
        return Ok(report);
    }

    [HttpGet("blocks")]
    public async Task<ActionResult<IReadOnlyList<BlockedUserResponse>>> GetMyBlocks(CancellationToken cancellationToken)
    {
        var blocks = await blockService.GetMyBlocksAsync(User.GetUserId(), cancellationToken);
        return Ok(blocks);
    }

    [HttpPost("blocks/{userId:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.ReportsPolicy)]
    public async Task<IActionResult> BlockUser(Guid userId, CancellationToken cancellationToken)
    {
        await blockService.BlockAsync(User.GetUserId(), userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("blocks/{userId:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.ReportsPolicy)]
    public async Task<IActionResult> UnblockUser(Guid userId, CancellationToken cancellationToken)
    {
        await blockService.UnblockAsync(User.GetUserId(), userId, cancellationToken);
        return NoContent();
    }
}
