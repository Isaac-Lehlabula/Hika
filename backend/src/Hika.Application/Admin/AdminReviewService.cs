using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Domain.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminReviewService(IAppDbContext db, IAuditLogger auditLogger) : IAdminReviewService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<AdminReviewSummaryResponse>> GetReviewsAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.Reviews.OrderByDescending(r => r.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        if (reviews.Count == 0)
        {
            return PagedResult<AdminReviewSummaryResponse>.Create([], page, pageSize, totalCount);
        }

        var userIds = reviews.Select(r => r.ReviewerUserId).Concat(reviews.Select(r => r.RevieweeUserId)).Distinct().ToList();
        var names = await db.UserProfiles.Where(p => userIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName}", cancellationToken);

        var responses = reviews.Select(r => new AdminReviewSummaryResponse
        {
            Id = r.Id,
            BookingId = r.BookingId,
            ReviewerName = names.GetValueOrDefault(r.ReviewerUserId, "Unknown"),
            RevieweeName = names.GetValueOrDefault(r.RevieweeUserId, "Unknown"),
            Direction = r.Direction.ToString(),
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAtUtc = r.CreatedAtUtc,
        }).ToList();

        return PagedResult<AdminReviewSummaryResponse>.Create(responses, page, pageSize, totalCount);
    }

    /// <summary>Deletes the review and reverses its contribution to the reviewee's cached
    /// rating aggregate — see UserProfile.RemoveCompletedTripReview — so moderation doesn't
    /// leave a stale average behind.</summary>
    public async Task DeleteAsync(Guid adminUserId, Guid reviewId, CancellationToken cancellationToken)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
            ?? throw new NotFoundException(nameof(Review), reviewId);

        var reviewee = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == review.RevieweeUserId, cancellationToken);
        reviewee?.RemoveCompletedTripReview(review.Rating);

        db.Reviews.Remove(review);
        auditLogger.Record(adminUserId, "DeleteReview", nameof(Review), reviewId, $"Rating {review.Rating}");
        await db.SaveChangesAsync(cancellationToken);
    }
}
