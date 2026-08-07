using Hika.Application.Admin.Dtos;

namespace Hika.Application.Admin;

public interface IAdminAnalyticsService
{
    Task<AnalyticsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken);
}
