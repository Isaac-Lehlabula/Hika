using Hika.Domain.Notifications;
using Shouldly;

namespace Hika.UnitTests.Domain.Notifications;

public class DeviceTokenTests
{
    [Fact]
    public void Register_CreatesATokenForTheUser()
    {
        var userId = Guid.NewGuid();

        var token = DeviceToken.Register(userId, "fcm-token-abc", DevicePlatform.Android);

        token.UserId.ShouldBe(userId);
        token.Token.ShouldBe("fcm-token-abc");
        token.Platform.ShouldBe(DevicePlatform.Android);
    }

    [Fact]
    public void ReassignTo_ChangesTheOwningUser()
    {
        var token = DeviceToken.Register(Guid.NewGuid(), "fcm-token-abc", DevicePlatform.Ios);
        var newUserId = Guid.NewGuid();

        token.ReassignTo(newUserId);

        token.UserId.ShouldBe(newUserId);
    }
}
