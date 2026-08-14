using Hika.Domain.Common;
using Hika.Domain.Payments;

namespace Hika.Application.Payments.Ports;

/// <summary>
/// A gateway can settle a request in one of two shapes: synchronously, in the same call
/// (MockPaymentGateway — auto-succeeds immediately, which is what makes it possible to build
/// and test the whole booking → payment flow without a real provider), or asynchronously via a
/// redirect the caller sends the payer to, with the actual outcome arriving later over a
/// webhook (OzowPaymentGateway — see docs/south-africa.md). IsPending is the discriminator:
/// when true, Succeeded/FailureReason aren't known yet and RedirectUrl is where to send the
/// payer; when false, the outcome is already final. See PaymentService.CompletePaymentAsync,
/// the one place both shapes converge.
/// </summary>
public sealed record PaymentInitiationResult
{
    public required bool IsPending { get; init; }

    public bool Succeeded { get; init; }

    public string? ProviderReference { get; init; }

    public string? RedirectUrl { get; init; }

    public string? FailureReason { get; init; }

    public static PaymentInitiationResult SettledImmediately(bool succeeded, string? providerReference, string? failureReason = null) =>
        new() { IsPending = false, Succeeded = succeeded, ProviderReference = providerReference, FailureReason = failureReason };

    public static PaymentInitiationResult Pending(string redirectUrl, string? providerReference) =>
        new() { IsPending = true, RedirectUrl = redirectUrl, ProviderReference = providerReference };
}

/// <summary>Same synchronous-vs-pending shape as <see cref="PaymentInitiationResult"/>, for
/// refunds — a redirect-based gateway's refund product is typically also notify-webhook based,
/// not instant.</summary>
public sealed record RefundInitiationResult
{
    public required bool IsPending { get; init; }

    public bool Succeeded { get; init; }

    public string? ProviderReference { get; init; }

    public string? FailureReason { get; init; }

    public static RefundInitiationResult SettledImmediately(bool succeeded, string? failureReason = null) =>
        new() { IsPending = false, Succeeded = succeeded, FailureReason = failureReason };

    public static RefundInitiationResult Pending(string? providerReference) =>
        new() { IsPending = true, ProviderReference = providerReference };
}

/// <summary>
/// MVP implementation was MockPaymentGateway (see Infrastructure) — auto-succeeds with a fake
/// reference, which made it possible to build and test the whole booking → payment flow before
/// a real South African provider was integrated. OzowPaymentGateway (see Infrastructure) is the
/// real one, added once Ozow was chosen from the docs/south-africa.md candidates. Both
/// implementations coexist — which one is registered is a one-line DI change (see
/// DependencyInjection.cs), and Mock remains what every automated test runs against, since
/// Ozow itself can't be exercised without live merchant credentials.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Lets PaymentService stamp the right PaymentProvider on a Payment/Refund row
    /// without depending on the concrete Infrastructure type behind this interface.</summary>
    PaymentProvider Provider { get; }

    /// <param name="bookingId">Doubles as the gateway-facing merchant reference for
    /// redirect-based gateways — Payment already has a unique index on BookingId, so no
    /// separate tracking column is needed to correlate a later webhook back to this payment.</param>
    Task<PaymentInitiationResult> InitiatePaymentAsync(Guid bookingId, Money amount, CancellationToken cancellationToken);

    /// <param name="refundId">Same role as bookingId above, but for correlating a refund
    /// webhook back to the Refund row that initiated it.</param>
    /// <param name="originalProviderReference">The gateway's own identifier for the payment
    /// being refunded (Payment.ProviderReference) — redirect-based refund APIs process against
    /// this, not our BookingId.</param>
    Task<RefundInitiationResult> InitiateRefundAsync(
        Guid refundId, string originalProviderReference, Money amount, CancellationToken cancellationToken);
}
