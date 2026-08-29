using Hika.Application.Notifications;
using Hika.Application.Notifications.Dtos;
using Hika.Domain.Notifications;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Notifications;

public class DeviceTokenServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly DeviceTokenService _sut;

    public DeviceTokenServiceTests()
    {
        _sut = new DeviceTokenService(_db);
    }

    [Fact]
    public async Task RegisterAsync_NewToken_CreatesARowForTheUser()
    {
        var userId = Guid.NewGuid();

        await _sut.RegisterAsync(userId, new RegisterDeviceTokenRequest { Token = "fcm-abc", Platform = DevicePlatform.Android }, CancellationToken.None);

        var stored = _db.DeviceTokens.Single();
        stored.UserId.ShouldBe(userId);
        stored.Token.ShouldBe("fcm-abc");
        stored.Platform.ShouldBe(DevicePlatform.Android);
    }

    [Fact]
    public async Task RegisterAsync_TokenAlreadyOwnedByAnotherUser_ReassignsRatherThanDuplicating()
    {
        var previousOwner = Guid.NewGuid();
        var newOwner = Guid.NewGuid();
        await _sut.RegisterAsync(previousOwner, new RegisterDeviceTokenRequest { Token = "shared-device", Platform = DevicePlatform.Ios }, CancellationToken.None);

        await _sut.RegisterAsync(newOwner, new RegisterDeviceTokenRequest { Token = "shared-device", Platform = DevicePlatform.Ios }, CancellationToken.None);

        _db.DeviceTokens.Count().ShouldBe(1);
        _db.DeviceTokens.Single().UserId.ShouldBe(newOwner);
    }

    [Fact]
    public async Task UnregisterAsync_RemovesTheCallersOwnToken()
    {
        var userId = Guid.NewGuid();
        await _sut.RegisterAsync(userId, new RegisterDeviceTokenRequest { Token = "fcm-abc", Platform = DevicePlatform.Web }, CancellationToken.None);

        await _sut.UnregisterAsync(userId, "fcm-abc", CancellationToken.None);

        _db.DeviceTokens.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnregisterAsync_TokenBelongsToAnotherUser_DoesNothing()
    {
        var owner = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        await _sut.RegisterAsync(owner, new RegisterDeviceTokenRequest { Token = "fcm-abc", Platform = DevicePlatform.Android }, CancellationToken.None);

        await _sut.UnregisterAsync(otherUser, "fcm-abc", CancellationToken.None);

        _db.DeviceTokens.Count().ShouldBe(1);
    }
}
