using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;

namespace Hika.Application.Admin;

public interface IAdminReviewService
{
    Task<PagedResult<AdminReviewSummaryResponse>> GetReviewsAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task DeleteAsync(Guid adminUserId, Guid reviewId, CancellationToken cancellationToken);
}
