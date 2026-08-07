namespace Hika.Application.Reviews.Dtos;

public sealed record ReviewResponse
{
    public required Guid Id { get; init; }

    public required Guid BookingId { get; init; }

    public required Guid ReviewerUserId { get; init; }

    public required string ReviewerFirstName { get; init; }

    public string? ReviewerPhotoUrl { get; init; }

    public required Guid RevieweeUserId { get; init; }

    public required string Direction { get; init; }

    public required int Rating { get; init; }

    public string? Comment { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
