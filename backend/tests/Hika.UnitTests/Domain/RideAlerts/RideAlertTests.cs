using Hika.Domain.RideAlerts;
using Shouldly;

namespace Hika.UnitTests.Domain.RideAlerts;

public class RideAlertTests
{
    [Fact]
    public void Create_SetsStatusToActive()
    {
        var alert = RideAlert.Create(Guid.NewGuid(), "Johannesburg", "Giyani", null);

        alert.Status.ShouldBe(RideAlertStatus.Active);
        alert.TravelDate.ShouldBeNull();
    }

    [Fact]
    public void MarkFulfilled_ActiveAlert_BecomesFulfilled()
    {
        var alert = RideAlert.Create(Guid.NewGuid(), "Johannesburg", "Giyani", null);

        alert.MarkFulfilled();

        alert.Status.ShouldBe(RideAlertStatus.Fulfilled);
    }

    [Fact]
    public void MarkFulfilled_AlreadyFulfilled_Throws()
    {
        var alert = RideAlert.Create(Guid.NewGuid(), "Johannesburg", "Giyani", null);
        alert.MarkFulfilled();

        Should.Throw<InvalidOperationException>(() => alert.MarkFulfilled());
    }

    [Fact]
    public void Cancel_ActiveAlert_BecomesCancelled()
    {
        var alert = RideAlert.Create(Guid.NewGuid(), "Johannesburg", "Giyani", null);

        alert.Cancel();

        alert.Status.ShouldBe(RideAlertStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyFulfilled_Throws()
    {
        var alert = RideAlert.Create(Guid.NewGuid(), "Johannesburg", "Giyani", null);
        alert.MarkFulfilled();

        Should.Throw<InvalidOperationException>(() => alert.Cancel());
    }
}
