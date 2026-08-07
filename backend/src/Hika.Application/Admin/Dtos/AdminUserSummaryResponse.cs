namespace Hika.Application.Admin.Dtos;

public sealed record AdminUserSummaryResponse
{
    public required Guid UserId { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required bool EmailVerified { get; init; }

    public required bool PhoneVerified { get; init; }

    public required bool IsAdmin { get; init; }

    public required bool IsSuspended { get; init; }

    public string? SuspensionReason { get; init; }

    public decimal? AverageRating { get; init; }

    public required int CompletedTripCount { get; init; }

    public required DateTimeOffset MemberSinceUtc { get; init; }
}
