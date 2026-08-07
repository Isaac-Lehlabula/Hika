using Hika.Domain.Common;

namespace Hika.Domain.Reviews;

public enum ReviewDirection
{
    PassengerToDriver,
    DriverToPassenger,
}

/// <summary>
/// One person's review of the other on a completed Booking — enforced only when
/// Booking.Status == Completed (an application-layer rule, not a DB constraint, since
/// "completed" is a state transition over time — see ReviewService). One review per person
/// per booking (BookingId, ReviewerUserId) is unique at the database level.
/// </summary>
public sealed class Review : AuditableEntity
{
    public Guid BookingId { get; private set; }

    public Guid ReviewerUserId { get; private set; }

    public Guid RevieweeUserId { get; private set; }

    public ReviewDirection Direction { get; private set; }

    public int Rating { get; private set; }

    public string? Comment { get; private set; }

    private Review()
    {
    }

    public static Review Submit(
        Guid bookingId, Guid reviewerUserId, Guid revieweeUserId, ReviewDirection direction, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "Rating must be between 1 and 5.");
        }

        return new Review
        {
            BookingId = bookingId,
            ReviewerUserId = reviewerUserId,
            RevieweeUserId = revieweeUserId,
            Direction = direction,
            Rating = rating,
            Comment = comment,
        };
    }
}
