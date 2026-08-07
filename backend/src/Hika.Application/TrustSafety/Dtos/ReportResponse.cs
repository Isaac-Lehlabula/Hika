namespace Hika.Application.TrustSafety.Dtos;

public sealed record ReportResponse
{
    public required Guid Id { get; init; }

    public Guid? ReportedUserId { get; init; }

    public Guid? ReportedTripId { get; init; }

    public required string Reason { get; init; }

    public required string Description { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
