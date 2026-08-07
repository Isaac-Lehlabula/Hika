using Hika.Api.Common;
using Hika.Application.Admin;
using Hika.Application.Admin.Dtos;
using Hika.Application.Payments.Dtos;
using Hika.Domain.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/payments")]
[Authorize(Policy = "Admin")]
public sealed class AdminPaymentsController(IAdminPaymentService adminPaymentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] PaymentStatus? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await adminPaymentService.GetPaymentsAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("bookings/{bookingId:guid}/refund")]
    public async Task<ActionResult<PaymentResponse>> Refund(
        Guid bookingId, AdminRefundRequest request, CancellationToken cancellationToken)
    {
        var payment = await adminPaymentService.RefundAsync(User.GetUserId(), bookingId, request.Reason, cancellationToken);
        return Ok(payment);
    }
}
