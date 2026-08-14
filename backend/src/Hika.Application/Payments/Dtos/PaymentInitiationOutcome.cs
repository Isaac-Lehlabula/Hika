namespace Hika.Application.Payments.Dtos;

/// <summary>What BookingService.AcceptAsync needs to know right after starting a payment — not
/// the outcome itself when IsPending (that arrives later via a webhook), just where to send the
/// caller. See PaymentService.InitiatePaymentAsync.</summary>
public sealed record PaymentInitiationOutcome
{
    public required bool IsPending { get; init; }

    /// <summary>Only meaningful when !IsPending — the gateway already resolved the payment
    /// (Mock) and PaymentService has already applied that to the Payment row; this just tells
    /// BookingService which way to move the Booking (ConfirmPayment vs FailPayment).</summary>
    public bool Succeeded { get; init; }

    public string? RedirectUrl { get; init; }
}
