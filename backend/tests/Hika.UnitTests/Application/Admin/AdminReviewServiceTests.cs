using Hika.Application.Admin;
using Hika.Domain.Reviews;
using Hika.Domain.Users;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Admin;

public class AdminReviewServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly AdminReviewService _sut;

    public AdminReviewServiceTests()
    {
        _sut = new AdminReviewService(_db, new AuditLogger(_db));
    }

    private (Review Review, UserProfile Reviewee) SeedReview()
    {
        var reviewer = UserProfile.Create(Guid.NewGuid(), "Thabo", "Nkosi");
        var reviewee = UserProfile.Create(Guid.NewGuid(), "Sipho", "Dlamini");
        reviewee.RecordCompletedTripReview(4m);
        reviewee.RecordCompletedTripReview(2m);
        _db.UserProfiles.AddRange(reviewer, reviewee);

        var review = Review.Submit(Guid.NewGuid(), reviewer.Id, reviewee.Id, ReviewDirection.PassengerToDriver, 2, "Late pickup");
        _db.Reviews.Add(review);
        _db.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        return (review, reviewee);
    }

    [Fact]
    public async Task GetReviewsAsync_ResolvesReviewerAndRevieweeNames()
    {
        SeedReview();

        var result = await _sut.GetReviewsAsync(page: 1, pageSize: 20, CancellationToken.None);

        result.Items.ShouldHaveSingleItem();
        result.Items[0].ReviewerName.ShouldBe("Thabo Nkosi");
        result.Items[0].RevieweeName.ShouldBe("Sipho Dlamini");
    }

    [Fact]
    public async Task DeleteAsync_RemovesReviewAndReversesRatingAggregate()
    {
        var (review, reviewee) = SeedReview();

        await _sut.DeleteAsync(Guid.NewGuid(), review.Id, CancellationToken.None);

        reviewee.AverageRating.ShouldBe(4m);
        reviewee.CompletedTripCount.ShouldBe(1);
        (await _db.Reviews.FindAsync(review.Id)).ShouldBeNull();
    }
}
