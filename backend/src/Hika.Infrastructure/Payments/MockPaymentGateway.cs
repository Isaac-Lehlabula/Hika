using Hika.Application.Payments.Ports;
using Hika.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Hika.Infrastructure.Payments;

/// <summary>MVP stand-in — always succeeds with a fake reference, logged instead of actually
/// moving money. See docs/south-africa.md for real SA providers to evaluate before launch.</summary>
public sealed class MockPaymentGateway(ILogger<MockPaymentGateway> logger) : IPaymentGateway
{
    public Task<PaymentGatewayResult> InitiateChargeAsync(Guid bookingId, Money amount, CancellationToken cancellationToken)
    {
        var reference = $"MOCK-{Guid.NewGuid():N}"[..17].ToUpperInvariant();
        logger.LogInformation(
            "[MOCK PAYMENT] Charged {Amount} {Currency} for booking {BookingId} — reference {Reference}",
            amount.Amount, amount.Currency, bookingId, reference);

        return Task.FromResult(new PaymentGatewayResult(Succeeded: true, ProviderReference: reference, FailureReason: null));
    }

    public Task<PaymentGatewayResult> RefundAsync(string providerReference, Money amount, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[MOCK PAYMENT] Refunded {Amount} {Currency} for original charge {Reference}",
            amount.Amount, amount.Currency, providerReference);

        return Task.FromResult(new PaymentGatewayResult(Succeeded: true, ProviderReference: providerReference, FailureReason: null));
    }
}
