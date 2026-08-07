using Hika.Application.Common.Persistence;
using Hika.Application.TrustSafety.Dtos;
using Hika.Domain.TrustSafety;

namespace Hika.Application.TrustSafety;

public sealed class ReportService(IAppDbContext db) : IReportService
{
    public async Task<ReportResponse> FileAsync(Guid reporterUserId, CreateReportRequest request, CancellationToken cancellationToken)
    {
        var report = Report.File(reporterUserId, request.ReportedUserId, request.ReportedTripId, request.Reason, request.Description);

        db.Reports.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        return new ReportResponse
        {
            Id = report.Id,
            ReportedUserId = report.ReportedUserId,
            ReportedTripId = report.ReportedTripId,
            Reason = report.Reason.ToString(),
            Description = report.Description,
            Status = report.Status.ToString(),
            CreatedAtUtc = report.CreatedAtUtc,
        };
    }
}
