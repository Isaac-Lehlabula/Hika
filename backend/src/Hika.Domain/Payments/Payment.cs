using Hika.Domain.Common;

namespace Hika.Domain.Payments;

public enum PaymentProvider
{
    Mock,
}

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded,
}

/// <summary>
/// One charge for a Booking, captured the moment the driver accepts it (see
/// BookingService.AcceptAsync / PaymentService.CapturePaymentAsync) — not at request time,
/// since a Pending booking might still be declined.
/// </summary>
public sealed class Payment : AuditableEntity
{
    public Guid BookingId { get; private set; }

    /// <summary>The full fare — what the passenger is charged.</summary>
    public Money Amount { get; private set; }

    public Money PlatformFee { get; private set; }

    /// <summary>Amount − PlatformFee. Not yet paid out — see docs/domain-model.md §6 on why
    /// Payout is a deliberately separate, deferred concept (many payments roll into one
    /// payout run later; no payout-triggering mechanism exists yet at this MVP stage).</summary>
    public Money DriverPayoutAmount { get; private set; }

    public PaymentProvider Provider { get; private set; }

    public string? ProviderReference { get; private set; }

    public PaymentStatus Status { get; private set; }

    private Payment()
    {
        Amount = Money.Zero();
        PlatformFee = Money.Zero();
        DriverPayoutAmount = Money.Zero();
    }

    /// <param name="feeRate">The platform's cut as a fraction (e.g. 0.15 for 15%) — see
    /// Hika.Domain.Admin.PlatformFeeSettings, the admin-configurable source of this value.</param>
    public static Payment Charge(Guid bookingId, Money fare, decimal feeRate)
    {
        var platformFee = new Money(decimal.Round(fare.Amount * feeRate, 2, MidpointRounding.ToEven), fare.Currency);
        var payout = fare - platformFee;

        return new Payment
        {
            BookingId = bookingId,
            Amount = fare,
            PlatformFee = platformFee,
            DriverPayoutAmount = payout,
            Provider = PaymentProvider.Mock,
            Status = PaymentStatus.Pending,
        };
    }

    public void MarkSucceeded(string providerReference)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark a {Status} payment as succeeded.");
        }

        Status = PaymentStatus.Succeeded;
        ProviderReference = providerReference;
    }

    public void MarkFailed()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot mark a {Status} payment as failed.");
        }

        Status = PaymentStatus.Failed;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
        {
            throw new InvalidOperationException($"Cannot refund a payment that is {Status}.");
        }

        Status = PaymentStatus.Refunded;
    }
}
