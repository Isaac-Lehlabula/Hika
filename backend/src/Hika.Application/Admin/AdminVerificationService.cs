using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Domain.Drivers;
using Hika.Domain.TrustSafety;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminVerificationService(IAppDbContext db, IAuditLogger auditLogger) : IAdminVerificationService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<AdminVerificationResponse>> GetQueueAsync(
        VerificationStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        // Defaults to the actual "review queue" (Pending) rather than every verification ever
        // submitted — an explicit ?status= is how staff browse Verified/Rejected history.
        var effectiveStatus = status ?? VerificationStatus.Pending;

        var query = db.Verifications.Where(v => v.Status == effectiveStatus).OrderBy(v => v.SubmittedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var verifications = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var responses = await BuildResponsesAsync(verifications, cancellationToken);
        return PagedResult<AdminVerificationResponse>.Create(responses, page, pageSize, totalCount);
    }

    public async Task<AdminVerificationResponse> ApproveAsync(Guid adminUserId, Guid verificationId, CancellationToken cancellationToken)
    {
        var verification = await db.Verifications.FirstOrDefaultAsync(v => v.Id == verificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Verification), verificationId);

        verification.Approve(adminUserId);
        await SyncSubjectCacheAsync(verification, isVerified: true, cancellationToken);

        auditLogger.Record(adminUserId, "ApproveVerification", nameof(Verification), verificationId, verification.Type.ToString());
        await db.SaveChangesAsync(cancellationToken);

        return (await BuildResponsesAsync([verification], cancellationToken))[0];
    }

    public async Task<AdminVerificationResponse> RejectAsync(
        Guid adminUserId, Guid verificationId, string reason, CancellationToken cancellationToken)
    {
        var verification = await db.Verifications.FirstOrDefaultAsync(v => v.Id == verificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Verification), verificationId);

        verification.Reject(adminUserId, reason);
        await SyncSubjectCacheAsync(verification, isVerified: false, cancellationToken);

        auditLogger.Record(adminUserId, "RejectVerification", nameof(Verification), verificationId, reason);
        await db.SaveChangesAsync(cancellationToken);

        return (await BuildResponsesAsync([verification], cancellationToken))[0];
    }

    /// <summary>Keeps DriverProfile.IsVerifiedDriver / Vehicle.IsVerified — the cheap cached
    /// flags read on every search result — in sync with the Verification row that's their
    /// source of truth. IdentityDocument verifications have no cache field to sync; the
    /// Verification row itself is the only place that type is tracked.</summary>
    private async Task SyncSubjectCacheAsync(Verification verification, bool isVerified, CancellationToken cancellationToken)
    {
        switch (verification.Type)
        {
            case VerificationType.DriverLicense:
                var driverProfile = await db.DriverProfiles.FirstOrDefaultAsync(p => p.Id == verification.SubjectId, cancellationToken);
                if (isVerified)
                {
                    driverProfile?.MarkVerified();
                }
                else
                {
                    driverProfile?.MarkUnverified();
                }

                break;

            case VerificationType.VehicleRegistration:
                var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == verification.SubjectId, cancellationToken);
                if (isVerified)
                {
                    vehicle?.MarkVerified();
                }
                else
                {
                    vehicle?.MarkUnverified();
                }

                break;

            case VerificationType.IdentityDocument:
                break;
        }
    }

    private async Task<IReadOnlyList<AdminVerificationResponse>> BuildResponsesAsync(
        IReadOnlyList<Verification> verifications, CancellationToken cancellationToken)
    {
        if (verifications.Count == 0)
        {
            return [];
        }

        var userSubjectIds = verifications.Where(v => v.SubjectType == VerificationSubjectType.User).Select(v => v.SubjectId).Distinct().ToList();
        var userNames = await db.UserProfiles
            .Where(p => userSubjectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName}", cancellationToken);

        var vehicleSubjectIds = verifications.Where(v => v.SubjectType == VerificationSubjectType.Vehicle).Select(v => v.SubjectId).Distinct().ToList();
        var vehicleNames = await db.Vehicles
            .Where(v => vehicleSubjectIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => $"{v.Make} {v.Model} ({v.RegistrationNumber})", cancellationToken);

        return verifications.Select(v => new AdminVerificationResponse
        {
            Id = v.Id,
            SubjectType = v.SubjectType.ToString(),
            SubjectId = v.SubjectId,
            SubjectDisplayName = v.SubjectType == VerificationSubjectType.User
                ? userNames.GetValueOrDefault(v.SubjectId)
                : vehicleNames.GetValueOrDefault(v.SubjectId),
            Type = v.Type.ToString(),
            Status = v.Status.ToString(),
            DocumentUrl = v.DocumentUrl,
            SubmittedAtUtc = v.SubmittedAtUtc,
            ReviewedAtUtc = v.ReviewedAtUtc,
            RejectionReason = v.RejectionReason,
        }).ToList();
    }
}
