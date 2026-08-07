using Hika.Application.Reviews;
using Hika.Domain.Reviews;
using Hika.Domain.Users;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Reviews;

// SubmitAsync materializes Booking/Trip (both carry a required Money ComplexProperty the EF
// InMemory provider can't shape a query for — same limitation documented throughout this test
// project), so it's covered by the Postgres integration tests instead. GetForUserAsync only
// touches Review/UserProfile, neither of which has a ComplexProperty, so it's safe here.
public class ReviewServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly ReviewService _sut;

    public ReviewServiceTests()
    {
        _sut = new ReviewService(_db);
    }

    private async Task<Guid> SeedReviewedUserAsync()
    {
        var userId = Guid.NewGuid();
        _db.UserProfiles.Add(UserProfile.Create(userId, "Thabo", "Mokoena"));
        await _db.SaveChangesAsync(CancellationToken.None);
        return userId;
    }

    private async Task SeedReviewerAsync(Guid reviewerId, string firstName)
    {
        _db.UserProfiles.Add(UserProfile.Create(reviewerId, firstName, "Tester"));
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetForUserAsync_NoReviews_ReturnsEmptyPage()
    {
        var userId = await SeedReviewedUserAsync();

        var result = await _sut.GetForUserAsync(userId, page: 1, pageSize: 20, CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyReviewsForThatUser()
    {
        var userId = await SeedReviewedUserAsync();
        var otherUserId = await SeedReviewedUserAsync();
        var reviewerId = Guid.NewGuid();
        await SeedReviewerAsync(reviewerId, "Naledi");

        _db.Reviews.Add(Review.Submit(Guid.NewGuid(), reviewerId, userId, ReviewDirection.PassengerToDriver, 5, "Great trip"));
        _db.Reviews.Add(Review.Submit(Guid.NewGuid(), reviewerId, otherUserId, ReviewDirection.PassengerToDriver, 3, "Fine"));
        await _db.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetForUserAsync(userId, page: 1, pageSize: 20, CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].Rating.ShouldBe(5);
        result.Items[0].ReviewerFirstName.ShouldBe("Naledi");
    }

    [Fact]
    public async Task GetForUserAsync_OrdersNewestFirst()
    {
        var userId = await SeedReviewedUserAsync();
        var reviewerId = Guid.NewGuid();
        await SeedReviewerAsync(reviewerId, "Naledi");

        var older = Review.Submit(Guid.NewGuid(), reviewerId, userId, ReviewDirection.PassengerToDriver, 4, "Older");
        _db.Reviews.Add(older);
        await _db.SaveChangesAsync(CancellationToken.None);

        var newer = Review.Submit(Guid.NewGuid(), reviewerId, userId, ReviewDirection.PassengerToDriver, 5, "Newer");
        _db.Reviews.Add(newer);
        await _db.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetForUserAsync(userId, page: 1, pageSize: 20, CancellationToken.None);

        result.Items[0].Comment.ShouldBe("Newer");
    }

    [Fact]
    public async Task GetForUserAsync_PaginatesResults()
    {
        var userId = await SeedReviewedUserAsync();
        var reviewerId = Guid.NewGuid();
        await SeedReviewerAsync(reviewerId, "Naledi");

        for (var i = 0; i < 5; i++)
        {
            _db.Reviews.Add(Review.Submit(Guid.NewGuid(), reviewerId, userId, ReviewDirection.PassengerToDriver, 5, $"Review {i}"));
        }
        await _db.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetForUserAsync(userId, page: 1, pageSize: 2, CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.TotalPages.ShouldBe(3);
    }
}
