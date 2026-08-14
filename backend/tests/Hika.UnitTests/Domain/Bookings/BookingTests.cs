using Hika.Domain.Bookings;
using Hika.Domain.Common;
using Shouldly;

namespace Hika.UnitTests.Domain.Bookings;

public class BookingTests
{
    private static Booking NewBooking(int seatsRequested = 2) =>
        Booking.Request(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), seatsRequested, new Money(600m), "Thabo Mokoena", "+27821234567");

    [Fact]
    public void Request_CreatesPendingBookingWithAccountHolderPassenger()
    {
        var booking = NewBooking();

        booking.Status.ShouldBe(BookingStatus.Pending);
        booking.Passengers.Count.ShouldBe(1);
        booking.Passengers.Single().IsAccountHolder.ShouldBeTrue();
        booking.Passengers.Single().FullName.ShouldBe("Thabo Mokoena");
    }

    [Fact]
    public void Request_ZeroSeats_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => NewBooking(seatsRequested: 0));
    }

    [Fact]
    public void Accept_PendingBooking_BecomesAwaitingPayment()
    {
        var booking = NewBooking();

        booking.Accept();

        booking.Status.ShouldBe(BookingStatus.AwaitingPayment);
        booking.RespondedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Accept_AlreadyAcceptedBooking_Throws()
    {
        var booking = NewBooking();
        booking.Accept();

        Should.Throw<InvalidOperationException>(() => booking.Accept());
    }

    [Fact]
    public void ConfirmPayment_AwaitingPaymentBooking_BecomesConfirmed()
    {
        var booking = NewBooking();
        booking.Accept();

        booking.ConfirmPayment();

        booking.Status.ShouldBe(BookingStatus.Confirmed);
    }

    [Fact]
    public void ConfirmPayment_PendingBooking_Throws()
    {
        var booking = NewBooking();

        Should.Throw<InvalidOperationException>(() => booking.ConfirmPayment());
    }

    [Fact]
    public void FailPayment_AwaitingPaymentBooking_BecomesDeclined()
    {
        var booking = NewBooking();
        booking.Accept();

        booking.FailPayment();

        booking.Status.ShouldBe(BookingStatus.Declined);
    }

    [Fact]
    public void FailPayment_PendingBooking_Throws()
    {
        var booking = NewBooking();

        Should.Throw<InvalidOperationException>(() => booking.FailPayment());
    }

    [Fact]
    public void Decline_PendingBooking_BecomesDeclined()
    {
        var booking = NewBooking();

        booking.Decline();

        booking.Status.ShouldBe(BookingStatus.Declined);
        booking.RespondedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Decline_AfterAccepted_Throws()
    {
        var booking = NewBooking();
        booking.Accept();

        Should.Throw<InvalidOperationException>(() => booking.Decline());
    }

    [Fact]
    public void Cancel_PendingBooking_BecomesCancelledWithReason()
    {
        var booking = NewBooking();

        booking.Cancel("Change of plans");

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.CancellationReason.ShouldBe("Change of plans");
        booking.CancelledAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Cancel_AwaitingPaymentBooking_BecomesCancelled()
    {
        var booking = NewBooking();
        booking.Accept();

        booking.Cancel(null);

        booking.Status.ShouldBe(BookingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ConfirmedBooking_BecomesCancelled()
    {
        var booking = NewBooking();
        booking.Accept();
        booking.ConfirmPayment();

        booking.Cancel(null);

        booking.Status.ShouldBe(BookingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyDeclinedBooking_Throws()
    {
        var booking = NewBooking();
        booking.Decline();

        Should.Throw<InvalidOperationException>(() => booking.Cancel(null));
    }

    [Fact]
    public void Complete_ConfirmedBooking_BecomesCompleted()
    {
        var booking = NewBooking();
        booking.Accept();
        booking.ConfirmPayment();

        booking.Complete();

        booking.Status.ShouldBe(BookingStatus.Completed);
    }

    [Fact]
    public void Complete_PendingBooking_Throws()
    {
        var booking = NewBooking();

        Should.Throw<InvalidOperationException>(() => booking.Complete());
    }
}
