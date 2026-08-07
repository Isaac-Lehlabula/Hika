using Hika.Api.Common;
using Hika.Application.Admin;
using Hika.Application.Admin.Dtos;
using Hika.Domain.TrustSafety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/verifications")]
[Authorize(Policy = "Admin")]
public sealed class AdminVerificationsController(IAdminVerificationService adminVerificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQueue(
        [FromQuery] VerificationStatus? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await adminVerificationService.GetQueueAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{verificationId:guid}/approve")]
    public async Task<ActionResult<AdminVerificationResponse>> Approve(Guid verificationId, CancellationToken cancellationToken)
    {
        var verification = await adminVerificationService.ApproveAsync(User.GetUserId(), verificationId, cancellationToken);
        return Ok(verification);
    }

    [HttpPost("{verificationId:guid}/reject")]
    public async Task<ActionResult<AdminVerificationResponse>> Reject(
        Guid verificationId, RejectVerificationRequest request, CancellationToken cancellationToken)
    {
        var verification = await adminVerificationService.RejectAsync(User.GetUserId(), verificationId, request.Reason, cancellationToken);
        return Ok(verification);
    }
}
