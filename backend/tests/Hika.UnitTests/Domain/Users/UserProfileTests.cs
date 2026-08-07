using Hika.Domain.Users;
using Shouldly;

namespace Hika.UnitTests.Domain.Users;

public class UserProfileTests
{
    [Fact]
    public void SetPhoneNumber_WhenNumberChanges_ResetsVerification()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");
        profile.SetPhoneNumber("+27821234567");
        profile.MarkPhoneVerified();

        profile.SetPhoneNumber("+27829999999");

        profile.PhoneVerifiedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void SetPhoneNumber_WhenNumberUnchanged_DoesNotResetVerification()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");
        profile.SetPhoneNumber("+27821234567");
        profile.MarkPhoneVerified();

        profile.SetPhoneNumber("+27821234567");

        profile.PhoneVerifiedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void RecordCompletedTripReview_FirstReview_SetsAverageToThatRating()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");

        profile.RecordCompletedTripReview(4m);

        profile.AverageRating.ShouldBe(4m);
        profile.CompletedTripCount.ShouldBe(1);
    }

    [Fact]
    public void RecordCompletedTripReview_SecondReview_AveragesWithPrevious()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");
        profile.RecordCompletedTripReview(4m);

        profile.RecordCompletedTripReview(2m);

        profile.AverageRating.ShouldBe(3m);
        profile.CompletedTripCount.ShouldBe(2);
    }
}
