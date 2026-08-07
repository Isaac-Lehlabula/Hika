using Hika.Domain.Reviews;
using Shouldly;

namespace Hika.UnitTests.Domain.Reviews;

public class ReviewTests
{
    [Fact]
    public void Submit_ValidRating_CreatesReview()
    {
        var review = Review.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ReviewDirection.PassengerToDriver, 5, "Great driver!");

        review.Rating.ShouldBe(5);
        review.Direction.ShouldBe(ReviewDirection.PassengerToDriver);
        review.Comment.ShouldBe("Great driver!");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Submit_RatingOutOfRange_Throws(int rating)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => Review.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ReviewDirection.DriverToPassenger, rating, null));
    }

    [Fact]
    public void Submit_NoComment_IsAllowed()
    {
        var review = Review.Submit(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ReviewDirection.DriverToPassenger, 3, null);

        review.Comment.ShouldBeNull();
    }
}
