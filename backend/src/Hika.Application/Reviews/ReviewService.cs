using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Application.Notifications;
using Hika.Application.Reviews.Dtos;
using Hika.Domain.Bookings;
using Hika.Domain.Notifications;
using Hika.Domain.Reviews;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Reviews;

public sealed class ReviewService(IAppDbContext db, INotificationDispatcher notificationDispatcher) : IReviewService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<ReviewResponse> SubmitAsync(
        Guid reviewerUserId, Guid bookingId, CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        if (booking.Status != BookingStatus.Completed)
        {
            throw new AppValidationException("bookingId", "You can only review a completed trip.");
        }

        var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == booking.TripId, cancellationToken)
            ?? throw new InvalidOperationException("Booking references a trip that no longer exists.");

        Guid revieweeUserId;
        ReviewDirection direction;
        if (reviewerUserId == booking.PassengerUserId)
        {
            revieweeUserId = trip.DriverProfileId;
            direction = ReviewDirection.PassengerToDriver;
        }
        else if (reviewerUserId == trip.DriverProfileId)
        {
            revieweeUserId = booking.PassengerUserId;
            direction = ReviewDirection.DriverToPassenger;
        }
        else
        {
            throw new NotFoundException(nameof(Booking), bookingId);
        }

        var alreadyReviewed = await db.Reviews.AnyAsync(
            r => r.BookingId == bookingId && r.ReviewerUserId == reviewerUserId, cancellationToken);
        if (alreadyReviewed)
        {
            throw new ConflictException("You've already reviewed this booking.");
        }

        var reviewerProfile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == reviewerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Reviewer has no profile.");
        var revieweeProfile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == revieweeUserId, cancellationToken)
            ?? throw new InvalidOperationException("Reviewee has no profile.");

        var review = Review.Submit(bookingId, reviewerUserId, revieweeUserId, direction, request.Rating, request.Comment);
        db.Reviews.Add(review);

        revieweeProfile.RecordCompletedTripReview(request.Rating);

        await notificationDispatcher.DispatchAsync(
            revieweeUserId,
            NotificationType.NewReview,
            $"{reviewerProfile.FirstName} left you a {request.Rating}-star review.",
            review.Id,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(review, reviewerProfile.FirstName, reviewerProfile.PhotoUrl);
    }

    public async Task<PagedResult<ReviewResponse>> GetForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.Reviews.Where(r => r.RevieweeUserId == userId).OrderByDescending(r => r.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        if (reviews.Count == 0)
        {
            return PagedResult<ReviewResponse>.Create([], page, pageSize, totalCount);
        }

        var reviewerIds = reviews.Select(r => r.ReviewerUserId).Distinct().ToList();
        var reviewerProfiles = await db.UserProfiles.Where(p => reviewerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        var responses = reviews
            .Select(r => reviewerProfiles.TryGetValue(r.ReviewerUserId, out var reviewer)
                ? ToResponse(r, reviewer.FirstName, reviewer.PhotoUrl)
                : ToResponse(r, "Former user", null))
            .ToList();

        return PagedResult<ReviewResponse>.Create(responses, page, pageSize, totalCount);
    }

    private static ReviewResponse ToResponse(Review review, string reviewerFirstName, string? reviewerPhotoUrl) => new()
    {
        Id = review.Id,
        BookingId = review.BookingId,
        ReviewerUserId = review.ReviewerUserId,
        ReviewerFirstName = reviewerFirstName,
        ReviewerPhotoUrl = reviewerPhotoUrl,
        RevieweeUserId = review.RevieweeUserId,
        Direction = review.Direction.ToString(),
        Rating = review.Rating,
        Comment = review.Comment,
        CreatedAtUtc = review.CreatedAtUtc,
    };
}
