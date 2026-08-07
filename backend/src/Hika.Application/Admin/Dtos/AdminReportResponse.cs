namespace Hika.Application.Admin.Dtos;

public sealed record AdminReportResponse
{
    public required Guid Id { get; init; }

    public required string ReporterName { get; init; }

    public required Guid ReporterUserId { get; init; }

    public string? ReportedUserName { get; init; }

    public Guid? ReportedUserId { get; init; }

    public Guid? ReportedTripId { get; init; }

    public required string Reason { get; init; }

    public required string Description { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
