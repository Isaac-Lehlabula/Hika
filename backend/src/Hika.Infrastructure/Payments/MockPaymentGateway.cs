using Hika.Application.Payments.Ports;
using Hika.Domain.Common;
using Hika.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace Hika.Infrastructure.Payments;

/// <summary>MVP/test stand-in — always settles immediately (IsPending: false) with a fake
/// reference, logged instead of actually moving money. What every automated test runs against,
/// since OzowPaymentGateway can't be exercised without live merchant credentials. See
/// docs/south-africa.md.</summary>
public sealed class MockPaymentGateway(ILogger<MockPaymentGateway> logger) : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Mock;

    public Task<PaymentInitiationResult> InitiatePaymentAsync(Guid bookingId, Money amount, CancellationToken cancellationToken)
    {
        var reference = $"MOCK-{Guid.NewGuid():N}"[..17].ToUpperInvariant();
        logger.LogInformation(
            "[MOCK PAYMENT] Charged {Amount} {Currency} for booking {BookingId} — reference {Reference}",
            amount.Amount, amount.Currency, bookingId, reference);

        return Task.FromResult(PaymentInitiationResult.SettledImmediately(succeeded: true, providerReference: reference));
    }

    public Task<RefundInitiationResult> InitiateRefundAsync(
        Guid refundId, string originalProviderReference, Money amount, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[MOCK PAYMENT] Refunded {Amount} {Currency} for original charge {Reference}",
            amount.Amount, amount.Currency, originalProviderReference);

        return Task.FromResult(RefundInitiationResult.SettledImmediately(succeeded: true));
    }
}
