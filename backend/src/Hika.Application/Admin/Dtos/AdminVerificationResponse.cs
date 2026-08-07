namespace Hika.Application.Admin.Dtos;

public sealed record AdminVerificationResponse
{
    public required Guid Id { get; init; }

    public required string SubjectType { get; init; }

    public required Guid SubjectId { get; init; }

    /// <summary>Resolved display name for the subject — the user's or vehicle owner's full
    /// name — so the review queue doesn't force staff to cross-reference a raw Guid.</summary>
    public string? SubjectDisplayName { get; init; }

    public required string Type { get; init; }

    public required string Status { get; init; }

    public string? DocumentUrl { get; init; }

    public DateTimeOffset? SubmittedAtUtc { get; init; }

    public DateTimeOffset? ReviewedAtUtc { get; init; }

    public string? RejectionReason { get; init; }
}
