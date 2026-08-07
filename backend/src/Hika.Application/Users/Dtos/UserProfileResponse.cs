namespace Hika.Application.Users.Dtos;

/// <summary>Own profile — includes private fields. Contrast with <see cref="PublicUserProfileResponse"/>.</summary>
public sealed record UserProfileResponse
{
    public required Guid UserId { get; init; }

    public required string Email { get; init; }

    public required bool EmailVerified { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? PhotoUrl { get; init; }

    public string? PhoneNumber { get; init; }

    public required bool PhoneVerified { get; init; }

    public required DateTimeOffset MemberSinceUtc { get; init; }

    public decimal? AverageRating { get; init; }

    public required int CompletedTripCount { get; init; }
}
