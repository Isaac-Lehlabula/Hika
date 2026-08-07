using Hika.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize(Policy = "Admin")]
public sealed class AdminAuditLogsController(IAdminAuditLogService adminAuditLogService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await adminAuditLogService.GetLogsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}
