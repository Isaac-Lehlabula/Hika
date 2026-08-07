using Hika.Domain.Common;
using Shouldly;

namespace Hika.UnitTests.Domain.Common;

public class MoneyTests
{
    [Fact]
    public void Constructor_RoundsToTwoDecimalPlaces()
    {
        var money = new Money(100.005m);

        money.Amount.ShouldBe(100.00m);
    }

    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Money(-1m));
    }

    [Fact]
    public void Addition_SameCurrency_Succeeds()
    {
        var result = new Money(200m) + new Money(70m);

        result.Amount.ShouldBe(270m);
    }

    [Fact]
    public void Subtraction_ComputesDriverPayout()
    {
        var fare = new Money(300m);
        var platformFee = new Money(30m);

        var payout = fare - platformFee;

        payout.Amount.ShouldBe(270m);
    }

    [Fact]
    public void Multiplication_BySeatCount_ScalesAmount()
    {
        var pricePerSeat = new Money(300m);

        var total = pricePerSeat * 3;

        total.Amount.ShouldBe(900m);
    }
}
