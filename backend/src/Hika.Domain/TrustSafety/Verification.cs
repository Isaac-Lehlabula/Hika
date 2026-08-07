using Hika.Domain.Common;

namespace Hika.Domain.TrustSafety;

public enum VerificationSubjectType
{
    User,
    Vehicle,
}

public enum VerificationType
{
    IdentityDocument,
    DriverLicense,
    VehicleRegistration,
}

public enum VerificationStatus
{
    NotSubmitted,
    Pending,
    Verified,
    Rejected,
}

/// <summary>
/// One table for every kind of verification (identity, driver's license, vehicle
/// registration) rather than a scattered boolean per concept — see docs/domain-model.md §9.
/// A future third-party verification provider integrates by writing to this one place;
/// for MVP an admin reviews DocumentUrl manually (admin review queue lands in Phase 11).
/// </summary>
public sealed class Verification : AuditableEntity
{
    public VerificationSubjectType SubjectType { get; private set; }

    public Guid SubjectId { get; private set; }

    public VerificationType Type { get; private set; }

    public VerificationStatus Status { get; private set; }

    public string? DocumentUrl { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public Guid? ReviewedByAdminUserId { get; private set; }

    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    public string? RejectionReason { get; private set; }

    private Verification()
    {
    }

    private Verification(VerificationSubjectType subjectType, Guid subjectId, VerificationType type)
    {
        SubjectType = subjectType;
        SubjectId = subjectId;
        Type = type;
        Status = VerificationStatus.NotSubmitted;
    }

    public static Verification CreateAndSubmit(
        VerificationSubjectType subjectType, Guid subjectId, VerificationType type, string documentUrl)
    {
        var verification = new Verification(subjectType, subjectId, type);
        verification.Submit(documentUrl);
        return verification;
    }

    public void Submit(string documentUrl)
    {
        DocumentUrl = documentUrl;
        Status = VerificationStatus.Pending;
        SubmittedAtUtc = DateTimeOffset.UtcNow;
        ReviewedByAdminUserId = null;
        ReviewedAtUtc = null;
        RejectionReason = null;
    }

    public void Approve(Guid adminUserId)
    {
        Status = VerificationStatus.Verified;
        ReviewedByAdminUserId = adminUserId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        RejectionReason = null;
    }

    public void Reject(Guid adminUserId, string reason)
    {
        Status = VerificationStatus.Rejected;
        ReviewedByAdminUserId = adminUserId;
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        RejectionReason = reason;
    }
}
