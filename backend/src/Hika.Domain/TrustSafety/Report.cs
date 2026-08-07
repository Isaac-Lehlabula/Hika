using Hika.Domain.Common;

namespace Hika.Domain.TrustSafety;

public enum ReportReason
{
    Harassment,
    UnsafeDriving,
    NoShow,
    Scam,
    Other,
}

public enum ReportStatus
{
    Open,
    UnderReview,
    Resolved,
    Dismissed,
}

/// <summary>A user or trip flagged for review. At least one of ReportedUserId/ReportedTripId is
/// always set — enforced by CreateReportRequestValidator, not a DB constraint, since "at least
/// one of two nullable columns" isn't cleanly expressible relationally.</summary>
public sealed class Report : AuditableEntity
{
    public Guid ReporterUserId { get; private set; }

    public Guid? ReportedUserId { get; private set; }

    public Guid? ReportedTripId { get; private set; }

    public ReportReason Reason { get; private set; }

    public string Description { get; private set; }

    public ReportStatus Status { get; private set; }

    private Report()
    {
        Description = string.Empty;
    }

    public static Report File(Guid reporterUserId, Guid? reportedUserId, Guid? reportedTripId, ReportReason reason, string description) =>
        new()
        {
            ReporterUserId = reporterUserId,
            ReportedUserId = reportedUserId,
            ReportedTripId = reportedTripId,
            Reason = reason,
            Description = description,
            Status = ReportStatus.Open,
        };
}
