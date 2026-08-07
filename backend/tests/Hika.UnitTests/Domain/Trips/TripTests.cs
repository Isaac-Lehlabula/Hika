using Hika.Domain.Common;
using Hika.Domain.Trips;
using Shouldly;

namespace Hika.UnitTests.Domain.Trips;

public class TripTests
{
    private static readonly IReadOnlyList<TripStopInput> ThreeStops =
    [
        new(null, "Johannesburg", Province.Gauteng),
        new(null, "Polokwane", Province.Limpopo),
        new(null, "Giyani", Province.Limpopo),
    ];

    private static Trip NewTrip(int totalSeatsOffered = 4, IReadOnlyList<TripStopInput>? stops = null) =>
        Trip.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1),
            totalSeatsOffered,
            new Money(300m),
            luggageAllowance: null,
            notes: null,
            stops ?? ThreeStops);

    [Fact]
    public void Create_FewerThanTwoStops_Throws()
    {
        Should.Throw<ArgumentException>(() => NewTrip(stops: [new(null, "Johannesburg", Province.Gauteng)]));
    }

    [Fact]
    public void Create_ZeroSeatsOffered_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => NewTrip(totalSeatsOffered: 0));
    }

    [Fact]
    public void Create_StopsAreOrderedBySequenceStartingAtZero()
    {
        var trip = NewTrip();

        trip.Stops.Select(s => s.Sequence).ShouldBe([0, 1, 2]);
        trip.Stops[0].RawName.ShouldBe("Johannesburg");
        trip.Stops[^1].RawName.ShouldBe("Giyani");
    }

    [Fact]
    public void Create_NStopsProduceNMinusOneAdjacentSegments()
    {
        var trip = NewTrip();

        trip.Segments.Count.ShouldBe(ThreeStops.Count - 1);
    }

    [Fact]
    public void Create_EachSegmentJoinsAdjacentStopsInOrder()
    {
        var trip = NewTrip();
        var stops = trip.Stops;

        var firstSegment = trip.Segments.Single(s => s.FromStopId == stops[0].Id);
        firstSegment.ToStopId.ShouldBe(stops[1].Id);

        var secondSegment = trip.Segments.Single(s => s.FromStopId == stops[1].Id);
        secondSegment.ToStopId.ShouldBe(stops[2].Id);
    }

    [Fact]
    public void Create_EverySegmentStartsWithFullSeatInventory()
    {
        var trip = NewTrip(totalSeatsOffered: 3);

        trip.Segments.ShouldAllBe(s => s.SeatsAvailable == 3);
    }

    [Fact]
    public void Cancel_ScheduledTrip_SetsStatusToCancelled()
    {
        var trip = NewTrip();

        trip.Cancel();

        trip.Status.ShouldBe(TripStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelledTrip_Throws()
    {
        var trip = NewTrip();
        trip.Cancel();

        Should.Throw<InvalidOperationException>(() => trip.Cancel());
    }
}
