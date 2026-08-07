using Hika.Domain.Common;
using Hika.Domain.Payments;
using Shouldly;

namespace Hika.UnitTests.Domain.Payments;

public class RefundTests
{
    [Fact]
    public void Request_CreatesPendingRefund()
    {
        var refund = Refund.Request(Guid.NewGuid(), new Money(300m), "Trip cancelled");

        refund.Status.ShouldBe(RefundStatus.Pending);
        refund.Amount.Amount.ShouldBe(300m);
        refund.Reason.ShouldBe("Trip cancelled");
    }

    [Fact]
    public void MarkSucceeded_PendingRefund_SetsStatusToSucceeded()
    {
        var refund = Refund.Request(Guid.NewGuid(), new Money(300m), "Trip cancelled");

        refund.MarkSucceeded();

        refund.Status.ShouldBe(RefundStatus.Succeeded);
    }

    [Fact]
    public void MarkSucceeded_AlreadySucceeded_Throws()
    {
        var refund = Refund.Request(Guid.NewGuid(), new Money(300m), "Trip cancelled");
        refund.MarkSucceeded();

        Should.Throw<InvalidOperationException>(() => refund.MarkSucceeded());
    }

    [Fact]
    public void MarkFailed_PendingRefund_SetsStatusToFailed()
    {
        var refund = Refund.Request(Guid.NewGuid(), new Money(300m), "Trip cancelled");

        refund.MarkFailed();

        refund.Status.ShouldBe(RefundStatus.Failed);
    }
}
