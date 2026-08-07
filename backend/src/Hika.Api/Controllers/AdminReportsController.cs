using Hika.Api.Common;
using Hika.Application.Admin;
using Hika.Application.Admin.Dtos;
using Hika.Domain.TrustSafety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Policy = "Admin")]
public sealed class AdminReportsController(IAdminReportService adminReportService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReports(
        [FromQuery] ReportStatus? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await adminReportService.GetReportsAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{reportId:guid}/resolve")]
    public async Task<ActionResult<AdminReportResponse>> Resolve(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await adminReportService.ResolveAsync(User.GetUserId(), reportId, cancellationToken);
        return Ok(report);
    }

    [HttpPost("{reportId:guid}/dismiss")]
    public async Task<ActionResult<AdminReportResponse>> Dismiss(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await adminReportService.DismissAsync(User.GetUserId(), reportId, cancellationToken);
        return Ok(report);
    }
}
