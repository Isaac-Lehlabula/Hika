using Hika.Application.Common.Pagination;
using Hika.Application.Reviews.Dtos;

namespace Hika.Application.Reviews;

public interface IReviewService
{
    Task<ReviewResponse> SubmitAsync(
        Guid reviewerUserId, Guid bookingId, CreateReviewRequest request, CancellationToken cancellationToken);

    Task<PagedResult<ReviewResponse>> GetForUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
}
