using Hika.Api.Common;
using Hika.Application.Payments;
using Hika.Application.Payments.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/bookings")]
[Authorize]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    [HttpGet("{bookingId:guid}/payment")]
    public async Task<ActionResult<PaymentResponse>> GetPayment(Guid bookingId, CancellationToken cancellationToken)
    {
        var payment = await paymentService.GetForBookingAsync(User.GetUserId(), bookingId, cancellationToken);
        return Ok(payment);
    }

    [HttpPost("{bookingId:guid}/refund")]
    public async Task<ActionResult<PaymentResponse>> RefundPayment(
        Guid bookingId, RefundRequest request, CancellationToken cancellationToken)
    {
        var payment = await paymentService.RefundAsync(User.GetUserId(), bookingId, request.Reason, cancellationToken);
        return Ok(payment);
    }
}
