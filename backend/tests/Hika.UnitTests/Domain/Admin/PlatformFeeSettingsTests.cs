using Hika.Domain.Admin;
using Shouldly;

namespace Hika.UnitTests.Domain.Admin;

public class PlatformFeeSettingsTests
{
    [Fact]
    public void CreateDefault_UsesTheWellKnownSingletonId()
    {
        var settings = PlatformFeeSettings.CreateDefault();

        settings.Id.ShouldBe(PlatformFeeSettings.SingletonId);
        settings.Rate.ShouldBe(PlatformFeeSettings.DefaultRate);
    }

    [Fact]
    public void UpdateRate_ValidRate_UpdatesRateAndAuditFields()
    {
        var settings = PlatformFeeSettings.CreateDefault();
        var adminUserId = Guid.NewGuid();

        settings.UpdateRate(0.2m, adminUserId);

        settings.Rate.ShouldBe(0.2m);
        settings.UpdatedByAdminUserId.ShouldBe(adminUserId);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void UpdateRate_OutOfRange_Throws(decimal rate)
    {
        var settings = PlatformFeeSettings.CreateDefault();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.UpdateRate(rate, Guid.NewGuid()));
    }
}
