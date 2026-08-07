namespace Hika.Application.Users.Dtos;

/// <summary>
/// What other users see (a driver's profile, a passenger's profile on a booking) — never
/// phone or email, per docs/security.md's PII-minimization rule.
/// </summary>
public sealed record PublicUserProfileResponse
{
    public required Guid UserId { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? PhotoUrl { get; init; }

    public required DateTimeOffset MemberSinceUtc { get; init; }

    public decimal? AverageRating { get; init; }

    public required int CompletedTripCount { get; init; }
}
