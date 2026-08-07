namespace Hika.Application.Admin.Dtos;

public sealed record AdminReviewSummaryResponse
{
    public required Guid Id { get; init; }

    public required Guid BookingId { get; init; }

    public required string ReviewerName { get; init; }

    public required string RevieweeName { get; init; }

    public required string Direction { get; init; }

    public required int Rating { get; init; }

    public string? Comment { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
