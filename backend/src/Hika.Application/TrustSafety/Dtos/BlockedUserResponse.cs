namespace Hika.Application.TrustSafety.Dtos;

public sealed record BlockedUserResponse
{
    public required Guid UserId { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? PhotoUrl { get; init; }

    public required DateTimeOffset BlockedAtUtc { get; init; }
}
