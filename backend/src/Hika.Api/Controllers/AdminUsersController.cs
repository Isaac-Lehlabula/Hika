using Hika.Api.Common;
using Hika.Application.Admin;
using Hika.Application.Admin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "Admin")]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await adminUserService.GetUsersAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{userId:guid}/suspend")]
    public async Task<ActionResult<AdminUserSummaryResponse>> SuspendUser(
        Guid userId, SuspendUserRequest request, CancellationToken cancellationToken)
    {
        var user = await adminUserService.SuspendAsync(User.GetUserId(), userId, request.Reason, cancellationToken);
        return Ok(user);
    }

    [HttpPost("{userId:guid}/unsuspend")]
    public async Task<ActionResult<AdminUserSummaryResponse>> UnsuspendUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await adminUserService.UnsuspendAsync(User.GetUserId(), userId, cancellationToken);
        return Ok(user);
    }
}
