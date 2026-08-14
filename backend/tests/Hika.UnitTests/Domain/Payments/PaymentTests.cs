using Hika.Domain.Admin;
using Hika.Domain.Common;
using Hika.Domain.Payments;
using Shouldly;

namespace Hika.UnitTests.Domain.Payments;

public class PaymentTests
{
    private const decimal FeeRate = PlatformFeeSettings.DefaultRate;

    [Fact]
    public void Charge_SplitsFareIntoPlatformFeeAndDriverPayout()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(1000m), FeeRate, PaymentProvider.Mock);

        payment.Amount.Amount.ShouldBe(1000m);
        payment.PlatformFee.Amount.ShouldBe(150m);
        payment.DriverPayoutAmount.Amount.ShouldBe(850m);
        payment.Status.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void Charge_PlatformFeePlusPayout_AlwaysEqualsFare()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(333.33m), FeeRate, PaymentProvider.Mock);

        (payment.PlatformFee.Amount + payment.DriverPayoutAmount.Amount).ShouldBe(payment.Amount.Amount);
    }

    [Fact]
    public void MarkSucceeded_PendingPayment_SetsStatusAndReference()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(300m), FeeRate, PaymentProvider.Mock);

        payment.MarkSucceeded("MOCK-ABC123");

        payment.Status.ShouldBe(PaymentStatus.Succeeded);
        payment.ProviderReference.ShouldBe("MOCK-ABC123");
    }

    [Fact]
    public void MarkSucceeded_AlreadySucceededPayment_Throws()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(300m), FeeRate, PaymentProvider.Mock);
        payment.MarkSucceeded("MOCK-ABC123");

        Should.Throw<InvalidOperationException>(() => payment.MarkSucceeded("MOCK-XYZ789"));
    }

    [Fact]
    public void MarkFailed_PendingPayment_SetsStatusToFailed()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(300m), FeeRate, PaymentProvider.Mock);

        payment.MarkFailed();

        payment.Status.ShouldBe(PaymentStatus.Failed);
    }

    [Fact]
    public void MarkRefunded_SucceededPayment_SetsStatusToRefunded()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(300m), FeeRate, PaymentProvider.Mock);
        payment.MarkSucceeded("MOCK-ABC123");

        payment.MarkRefunded();

        payment.Status.ShouldBe(PaymentStatus.Refunded);
    }

    [Fact]
    public void MarkRefunded_PendingPayment_Throws()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(300m), FeeRate, PaymentProvider.Mock);

        Should.Throw<InvalidOperationException>(() => payment.MarkRefunded());
    }

    [Fact]
    public void SetRedirectUrl_PendingPayment_SetsUrl()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(300m), FeeRate, PaymentProvider.Ozow);

        payment.SetRedirectUrl("https://pay.ozow.com/abc123");

        payment.RedirectUrl.ShouldBe("https://pay.ozow.com/abc123");
    }

    [Fact]
    public void MarkSucceeded_ClearsRedirectUrl()
    {
        var payment = Payment.Charge(Guid.NewGuid(), new Money(300m), FeeRate, PaymentProvider.Ozow);
        payment.SetRedirectUrl("https://pay.ozow.com/abc123");

        payment.MarkSucceeded("OZOW-TXN-1");

        payment.RedirectUrl.ShouldBeNull();
    }
}
