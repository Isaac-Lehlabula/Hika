using Hika.Domain.RideRequests;
using Shouldly;

namespace Hika.UnitTests.Domain.RideRequests;

public class RideRequestTests
{
    private static RideRequest NewRequest(int seatsNeeded = 2, decimal? proposedPricePerSeat = null) =>
        RideRequest.Post(Guid.NewGuid(), "Johannesburg", "Giyani", new DateOnly(2026, 12, 20), seatsNeeded, proposedPricePerSeat);

    [Fact]
    public void Post_CreatesAnOpenRequest()
    {
        var request = NewRequest();

        request.Status.ShouldBe(RideRequestStatus.Open);
        request.ClaimedByDriverUserId.ShouldBeNull();
        request.ClaimedBookingId.ShouldBeNull();
    }

    [Fact]
    public void Post_ZeroSeats_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => NewRequest(seatsNeeded: 0));
    }

    [Fact]
    public void Post_NegativeProposedPrice_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => NewRequest(proposedPricePerSeat: -50m));
    }

    [Fact]
    public void Post_NullProposedPrice_Succeeds()
    {
        var request = NewRequest(proposedPricePerSeat: null);

        request.ProposedPricePerSeat.ShouldBeNull();
    }

    [Fact]
    public void Claim_OpenRequest_BecomesClaimedWithDriverAndBooking()
    {
        var request = NewRequest();
        var driverId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        request.Claim(driverId, bookingId);

        request.Status.ShouldBe(RideRequestStatus.Claimed);
        request.ClaimedByDriverUserId.ShouldBe(driverId);
        request.ClaimedBookingId.ShouldBe(bookingId);
    }

    [Fact]
    public void Claim_AlreadyClaimedRequest_Throws()
    {
        var request = NewRequest();
        request.Claim(Guid.NewGuid(), Guid.NewGuid());

        Should.Throw<InvalidOperationException>(() => request.Claim(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Cancel_OpenRequest_BecomesCancelled()
    {
        var request = NewRequest();

        request.Cancel();

        request.Status.ShouldBe(RideRequestStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ClaimedRequest_Throws()
    {
        var request = NewRequest();
        request.Claim(Guid.NewGuid(), Guid.NewGuid());

        Should.Throw<InvalidOperationException>(() => request.Cancel());
    }
}
