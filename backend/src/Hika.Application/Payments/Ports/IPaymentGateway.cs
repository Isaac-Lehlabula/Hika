using Hika.Domain.Common;

namespace Hika.Application.Payments.Ports;

public sealed record PaymentGatewayResult(bool Succeeded, string? ProviderReference, string? FailureReason);

/// <summary>
/// MVP implementation is MockPaymentGateway (see Infrastructure) — auto-succeeds with a fake
/// reference, which is what makes it possible to build and test the whole booking → payment
/// flow before a real South African provider (PayFast, Yoco, Peach Payments, Ozow — see
/// docs/south-africa.md) is integrated. Swapping the implementation is a one-line DI change.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentGatewayResult> InitiateChargeAsync(Guid bookingId, Money amount, CancellationToken cancellationToken);

    Task<PaymentGatewayResult> RefundAsync(string providerReference, Money amount, CancellationToken cancellationToken);
}
