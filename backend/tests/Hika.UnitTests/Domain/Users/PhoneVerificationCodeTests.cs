using Hika.Domain.Users;
using Shouldly;

namespace Hika.UnitTests.Domain.Users;

public class PhoneVerificationCodeTests
{
    [Fact]
    public void IsUsable_WhenFresh_ReturnsTrue()
    {
        var code = new PhoneVerificationCode(Guid.NewGuid(), "+27821234567", "hash", DateTimeOffset.UtcNow.AddMinutes(10));

        code.IsUsable.ShouldBeTrue();
    }

    [Fact]
    public void IsUsable_AfterFiveFailedAttempts_ReturnsFalse()
    {
        var code = new PhoneVerificationCode(Guid.NewGuid(), "+27821234567", "hash", DateTimeOffset.UtcNow.AddMinutes(10));

        for (var i = 0; i < 5; i++)
        {
            code.RecordAttempt();
        }

        code.IsUsable.ShouldBeFalse();
    }

    [Fact]
    public void IsUsable_AfterMarkedUsed_ReturnsFalse()
    {
        var code = new PhoneVerificationCode(Guid.NewGuid(), "+27821234567", "hash", DateTimeOffset.UtcNow.AddMinutes(10));

        code.MarkUsed();

        code.IsUsable.ShouldBeFalse();
    }

    [Fact]
    public void IsUsable_WhenExpired_ReturnsFalse()
    {
        var code = new PhoneVerificationCode(Guid.NewGuid(), "+27821234567", "hash", DateTimeOffset.UtcNow.AddSeconds(-1));

        code.IsUsable.ShouldBeFalse();
    }
}
