using Hika.Api.Common;
using Hika.Application.TrustSafety;
using Hika.Application.TrustSafety.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/trust-safety")]
[Authorize]
public sealed class TrustSafetyController(IReportService reportService, IBlockService blockService) : ControllerBase
{
    [HttpPost("reports")]
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
    public async Task<IActionResult> BlockUser(Guid userId, CancellationToken cancellationToken)
    {
        await blockService.BlockAsync(User.GetUserId(), userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("blocks/{userId:guid}")]
    public async Task<IActionResult> UnblockUser(Guid userId, CancellationToken cancellationToken)
    {
        await blockService.UnblockAsync(User.GetUserId(), userId, cancellationToken);
        return NoContent();
    }
}
