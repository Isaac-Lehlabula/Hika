using Microsoft.Extensions.Options;

namespace Hika.Infrastructure.Payments.Ozow;

/// <summary>
/// Verifies the Hash field on an incoming Ozow notify-webhook payload — the only thing standing
/// between "a real payment update" and "anyone who finds the NotifyUrl can mark any booking
/// paid," since these endpoints are necessarily [AllowAnonymous] (see OzowWebhooksController).
/// A concrete Infrastructure dependency, not hidden behind an Application-layer port: this is
/// entirely Ozow-specific plumbing with exactly one consumer, so an interface here would be
/// abstraction without a second implementation to justify it.
/// </summary>
public sealed class OzowNotifyVerifier(IOptions<OzowOptions> options)
{
    public bool VerifyPaymentNotify(OzowPaymentNotifyPayload payload)
    {
        var expected = OzowHashHelper.ComputeHash(
            [
                payload.SiteCode, payload.TransactionId, payload.TransactionReference,
                payload.Amount, payload.Status, payload.CurrencyCode, payload.IsTest,
            ],
            options.Value.PrivateKey);

        return string.Equals(expected, payload.Hash, StringComparison.OrdinalIgnoreCase);
    }

    public bool VerifyRefundNotify(OzowRefundNotifyPayload payload)
    {
        var expected = OzowHashHelper.ComputeHash(
            [payload.SiteCode, payload.RefundId, payload.RefundReference, payload.Amount, payload.Status, payload.IsTest],
            options.Value.PrivateKey);

        return string.Equals(expected, payload.Hash, StringComparison.OrdinalIgnoreCase);
    }
}
