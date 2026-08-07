using Hika.Domain.Common;

namespace Hika.Domain.Trips;

/// <summary>
/// The atomic seat-inventory unit — one row per adjacent stop pair. SeatsAvailable is the
/// live, authoritative counter for that leg; a booking across multiple stops consumes every
/// segment it spans (see BookingSegment, Phase 6) and docs/domain-model.md §4.
/// </summary>
public sealed class TripSegment : Entity
{
    public Guid TripId { get; private set; }

    public Guid FromStopId { get; private set; }

    public Guid ToStopId { get; private set; }

    public int SeatsAvailable { get; private set; }

    /// <summary>Reserved for future per-segment pricing — unused in MVP (flat Trip.PricePerSeat).</summary>
    public Money? PriceOverride { get; private set; }

    private TripSegment()
    {
    }

    internal TripSegment(Guid tripId, Guid fromStopId, Guid toStopId, int seatsAvailable)
    {
        TripId = tripId;
        FromStopId = fromStopId;
        ToStopId = toStopId;
        SeatsAvailable = seatsAvailable;
    }

    internal void Reserve(int seats)
    {
        if (seats > SeatsAvailable)
        {
            throw new InvalidOperationException("Not enough seats available on this segment.");
        }

        SeatsAvailable -= seats;
    }

    internal void Release(int seats) => SeatsAvailable += seats;
}
