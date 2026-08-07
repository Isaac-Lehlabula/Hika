namespace Hika.Application.Reviews.Dtos;

public sealed record CreateReviewRequest
{
    public required int Rating { get; init; }

    public string? Comment { get; init; }
}
