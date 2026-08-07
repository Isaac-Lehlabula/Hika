using Hika.Domain.Common;

namespace Hika.Domain.Users;

/// <summary>
/// The profile/domain data for a user, separate from <c>ApplicationUser</c> (which is an
/// Infrastructure/Identity persistence concern — see docs/architecture.md §4). Keyed by the
/// same Guid as the identity user (a shared-primary-key 1:1 relationship), referenced only
/// by that Guid, never by a navigation property, so this project has no dependency on Identity.
/// </summary>
public sealed class UserProfile : Entity
{
    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string? PhotoUrl { get; private set; }

    public string? PhoneNumber { get; private set; }

    public DateTimeOffset? PhoneVerifiedAtUtc { get; private set; }

    public DateTimeOffset? EmailVerifiedAtUtc { get; private set; }

    public DateTimeOffset MemberSinceUtc { get; private set; }

    public decimal? AverageRating { get; private set; }

    public int CompletedTripCount { get; private set; }

    public bool IsAdmin { get; private set; }

    public bool IsSuspended { get; private set; }

    public DateTimeOffset? SuspendedAtUtc { get; private set; }

    public string? SuspensionReason { get; private set; }

    private UserProfile(Guid userId, string firstName, string lastName)
    {
        Id = userId;
        FirstName = firstName;
        LastName = lastName;
        MemberSinceUtc = DateTimeOffset.UtcNow;
        CompletedTripCount = 0;
    }

    /// <remarks>Required by EF Core materialization.</remarks>
    private UserProfile()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    public static UserProfile Create(Guid userId, string firstName, string lastName) =>
        new(userId, firstName, lastName);

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void UpdatePhoto(string? photoUrl) => PhotoUrl = photoUrl;

    /// <summary>Changing the phone number always requires re-verification.</summary>
    public void SetPhoneNumber(string phoneNumber)
    {
        if (phoneNumber == PhoneNumber)
        {
            return;
        }

        PhoneNumber = phoneNumber;
        PhoneVerifiedAtUtc = null;
    }

    public void MarkPhoneVerified() => PhoneVerifiedAtUtc = DateTimeOffset.UtcNow;

    public void MarkEmailVerified() => EmailVerifiedAtUtc = DateTimeOffset.UtcNow;

    public void RecordCompletedTripReview(decimal newRating)
    {
        var totalRatingPoints = (AverageRating ?? 0m) * CompletedTripCount;
        CompletedTripCount++;
        AverageRating = decimal.Round((totalRatingPoints + newRating) / CompletedTripCount, 2);
    }

    /// <summary>Reverses RecordCompletedTripReview — used when an admin deletes a review
    /// (moderation), so the aggregate stays consistent with the reviews that remain.</summary>
    public void RemoveCompletedTripReview(decimal rating)
    {
        if (CompletedTripCount <= 1)
        {
            CompletedTripCount = 0;
            AverageRating = null;
            return;
        }

        var totalRatingPoints = (AverageRating ?? 0m) * CompletedTripCount;
        CompletedTripCount--;
        AverageRating = decimal.Round((totalRatingPoints - rating) / CompletedTripCount, 2);
    }

    /// <summary>Not exposed via any API — a trusted-small-group admin is bootstrapped
    /// directly (see Program.cs's dev-only Admin:BootstrapEmail step), never self-service,
    /// to avoid a privilege-escalation endpoint. See docs/admin-portal.md.</summary>
    public void GrantAdmin() => IsAdmin = true;

    public void Suspend(string reason)
    {
        IsSuspended = true;
        SuspendedAtUtc = DateTimeOffset.UtcNow;
        SuspensionReason = reason;
    }

    public void Unsuspend()
    {
        IsSuspended = false;
        SuspendedAtUtc = null;
        SuspensionReason = null;
    }
}
