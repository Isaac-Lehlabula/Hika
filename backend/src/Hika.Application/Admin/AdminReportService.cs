using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Domain.TrustSafety;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminReportService(IAppDbContext db, IAuditLogger auditLogger) : IAdminReportService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<AdminReportResponse>> GetReportsAsync(
        ReportStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.Reports.AsQueryable();
        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        query = query.OrderByDescending(r => r.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var reports = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var responses = await BuildResponsesAsync(reports, cancellationToken);
        return PagedResult<AdminReportResponse>.Create(responses, page, pageSize, totalCount);
    }

    public async Task<AdminReportResponse> ResolveAsync(Guid adminUserId, Guid reportId, CancellationToken cancellationToken)
    {
        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken)
            ?? throw new NotFoundException(nameof(Report), reportId);

        report.Resolve();
        auditLogger.Record(adminUserId, "ResolveReport", nameof(Report), reportId, null);
        await db.SaveChangesAsync(cancellationToken);

        return (await BuildResponsesAsync([report], cancellationToken))[0];
    }

    public async Task<AdminReportResponse> DismissAsync(Guid adminUserId, Guid reportId, CancellationToken cancellationToken)
    {
        var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken)
            ?? throw new NotFoundException(nameof(Report), reportId);

        report.Dismiss();
        auditLogger.Record(adminUserId, "DismissReport", nameof(Report), reportId, null);
        await db.SaveChangesAsync(cancellationToken);

        return (await BuildResponsesAsync([report], cancellationToken))[0];
    }

    private async Task<IReadOnlyList<AdminReportResponse>> BuildResponsesAsync(
        IReadOnlyList<Report> reports, CancellationToken cancellationToken)
    {
        if (reports.Count == 0)
        {
            return [];
        }

        var userIds = reports.Select(r => r.ReporterUserId)
            .Concat(reports.Where(r => r.ReportedUserId.HasValue).Select(r => r.ReportedUserId!.Value))
            .Distinct()
            .ToList();
        var names = await db.UserProfiles.Where(p => userIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName}", cancellationToken);

        return reports.Select(r => new AdminReportResponse
        {
            Id = r.Id,
            ReporterName = names.GetValueOrDefault(r.ReporterUserId, "Unknown"),
            ReporterUserId = r.ReporterUserId,
            ReportedUserName = r.ReportedUserId.HasValue ? names.GetValueOrDefault(r.ReportedUserId.Value) : null,
            ReportedUserId = r.ReportedUserId,
            ReportedTripId = r.ReportedTripId,
            Reason = r.Reason.ToString(),
            Description = r.Description,
            Status = r.Status.ToString(),
            CreatedAtUtc = r.CreatedAtUtc,
        }).ToList();
    }
}
