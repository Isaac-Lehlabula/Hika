using Hika.Domain.TrustSafety;

namespace Hika.Application.TrustSafety.Dtos;

public sealed record CreateReportRequest
{
    public Guid? ReportedUserId { get; init; }

    public Guid? ReportedTripId { get; init; }

    public required ReportReason Reason { get; init; }

    public required string Description { get; init; }
}
