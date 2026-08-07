using Hika.Domain.Users;
using Shouldly;

namespace Hika.UnitTests.Domain.Users;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_WhenFresh_ReturnsTrue()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(30), null, null);

        token.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddSeconds(-1), null, null);

        token.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void IsActive_WhenRevoked_ReturnsFalse()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(30), null, null);

        token.Revoke();

        token.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Revoke_SetsReplacedByTokenHash_WhenRotated()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(30), null, null);

        token.Revoke("new-hash");

        token.ReplacedByTokenHash.ShouldBe("new-hash");
        token.RevokedAtUtc.ShouldNotBeNull();
    }
}
