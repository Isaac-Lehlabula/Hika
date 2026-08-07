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

    [Fact]
    public void RemoveCompletedTripReview_OnlyReview_ClearsAverage()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");
        profile.RecordCompletedTripReview(4m);

        profile.RemoveCompletedTripReview(4m);

        profile.AverageRating.ShouldBeNull();
        profile.CompletedTripCount.ShouldBe(0);
    }

    [Fact]
    public void RemoveCompletedTripReview_OneOfSeveral_ReversesTheAverage()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");
        profile.RecordCompletedTripReview(4m);
        profile.RecordCompletedTripReview(2m);

        profile.RemoveCompletedTripReview(2m);

        profile.AverageRating.ShouldBe(4m);
        profile.CompletedTripCount.ShouldBe(1);
    }

    [Fact]
    public void Suspend_SetsSuspendedStateAndReason()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");

        profile.Suspend("Repeated no-shows");

        profile.IsSuspended.ShouldBeTrue();
        profile.SuspensionReason.ShouldBe("Repeated no-shows");
        profile.SuspendedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Unsuspend_ClearsSuspendedState()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");
        profile.Suspend("Repeated no-shows");

        profile.Unsuspend();

        profile.IsSuspended.ShouldBeFalse();
        profile.SuspensionReason.ShouldBeNull();
        profile.SuspendedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void GrantAdmin_SetsIsAdmin()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");

        profile.GrantAdmin();

        profile.IsAdmin.ShouldBeTrue();
    }
}
