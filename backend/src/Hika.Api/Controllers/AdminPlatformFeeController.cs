using Hika.Api.Common;
using Hika.Application.Admin;
using Hika.Application.Admin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/platform-fees")]
[Authorize(Policy = "Admin")]
public sealed class AdminPlatformFeeController(IAdminPlatformFeeService adminPlatformFeeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformFeeResponse>> Get(CancellationToken cancellationToken)
    {
        var settings = await adminPlatformFeeService.GetAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<PlatformFeeResponse>> Update(UpdatePlatformFeeRequest request, CancellationToken cancellationToken)
    {
        var settings = await adminPlatformFeeService.UpdateAsync(User.GetUserId(), request.Rate, cancellationToken);
        return Ok(settings);
    }
}
