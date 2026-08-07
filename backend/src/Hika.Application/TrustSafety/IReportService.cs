using Hika.Application.TrustSafety.Dtos;

namespace Hika.Application.TrustSafety;

public interface IReportService
{
    Task<ReportResponse> FileAsync(Guid reporterUserId, CreateReportRequest request, CancellationToken cancellationToken);
}
