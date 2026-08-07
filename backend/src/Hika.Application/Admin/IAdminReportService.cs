using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;
using Hika.Domain.TrustSafety;

namespace Hika.Application.Admin;

public interface IAdminReportService
{
    Task<PagedResult<AdminReportResponse>> GetReportsAsync(
        ReportStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<AdminReportResponse> ResolveAsync(Guid adminUserId, Guid reportId, CancellationToken cancellationToken);

    Task<AdminReportResponse> DismissAsync(Guid adminUserId, Guid reportId, CancellationToken cancellationToken);
}
