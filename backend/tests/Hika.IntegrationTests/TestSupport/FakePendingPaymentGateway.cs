using Hika.Application.Payments.Ports;
using Hika.Domain.Common;
using Hika.Domain.Payments;

namespace Hika.IntegrationTests.TestSupport;

/// <summary>
/// A redirect-based gateway double — always returns IsPending, no real network calls — so tests
/// can exercise the Ozow-shaped flow (AwaitingPayment booking, a RedirectUrl, resolution only
/// via a webhook) end-to-end against real Postgres without depending on Ozow's actual API. See
/// PendingPaymentGatewayFactory, which swaps this in for IPaymentGateway.
/// </summary>
public sealed class FakePendingPaymentGateway : IPaymentGateway
{
    public PaymentProvider Provider => PaymentProvider.Ozow;

    public Task<PaymentInitiationResult> InitiatePaymentAsync(Guid bookingId, Money amount, CancellationToken cancellationToken) =>
        Task.FromResult(PaymentInitiationResult.Pending($"https://fake-ozow.test/pay/{bookingId}", bookingId.ToString()));

    public Task<RefundInitiationResult> InitiateRefundAsync(
        Guid refundId, string originalProviderReference, Money amount, CancellationToken cancellationToken) =>
        Task.FromResult(RefundInitiationResult.Pending(refundId.ToString()));
}
