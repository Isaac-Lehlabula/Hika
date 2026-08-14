using Hika.Application.Bookings;
using Hika.Application.Payments;
using Hika.Infrastructure.Payments.Ozow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

/// <summary>
/// Server-to-server callbacks from Ozow — necessarily [AllowAnonymous] (Ozow doesn't hold one
/// of our JWTs), so OzowNotifyVerifier's hash check is the only thing standing between "a real
/// payment update" and "anyone who finds this URL can mark any booking paid." Every action
/// verifies the hash before touching anything and returns 400 (not an exception) when it
/// doesn't match, so a guessed-wrong field order (see OzowPaymentGateway's remarks) fails
/// closed instead of silently accepting an unverifiable payload.
/// </summary>
[ApiController]
[Route("api/v1/webhooks/ozow")]
[AllowAnonymous]
public sealed class OzowWebhooksController(
    IBookingService bookingService, IPaymentService paymentService, OzowNotifyVerifier verifier) : ControllerBase
{
    [HttpPost("payment-notify")]
    public async Task<IActionResult> PaymentNotify([FromForm] OzowPaymentNotifyPayload payload, CancellationToken cancellationToken)
    {
        if (!verifier.VerifyPaymentNotify(payload))
        {
            return BadRequest("Hash verification failed.");
        }

        if (!Guid.TryParse(payload.TransactionReference, out var bookingId))
        {
            return BadRequest("TransactionReference was not a recognisable booking id.");
        }

        // Ozow's documented status values include "Complete" for a successful payment;
        // everything else (Cancelled, Error, Abandoned, PendingInvestigation, ...) is treated
        // as not-succeeded — see OzowPaymentGateway's remarks on unverified specifics.
        var succeeded = string.Equals(payload.Status, "Complete", StringComparison.OrdinalIgnoreCase);

        await bookingService.ResolvePaymentOutcomeAsync(bookingId, succeeded, payload.TransactionId, cancellationToken);

        return Ok();
    }

    [HttpPost("refund-notify")]
    public async Task<IActionResult> RefundNotify([FromForm] OzowRefundNotifyPayload payload, CancellationToken cancellationToken)
    {
        if (!verifier.VerifyRefundNotify(payload))
        {
            return BadRequest("Hash verification failed.");
        }

        if (!Guid.TryParse(payload.RefundReference, out var refundId))
        {
            return BadRequest("RefundReference was not a recognisable refund id.");
        }

        var succeeded = string.Equals(payload.Status, "Complete", StringComparison.OrdinalIgnoreCase);

        await paymentService.ResolveRefundAsync(refundId, succeeded, cancellationToken);

        return Ok();
    }
}
