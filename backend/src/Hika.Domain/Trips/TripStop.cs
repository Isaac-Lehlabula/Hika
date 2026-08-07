using Hika.Domain.Common;

namespace Hika.Domain.Trips;

/// <summary>Ordered waypoint on a Trip (sequence 0..N-1); origin = 0, destination = N-1.</summary>
public sealed class TripStop : Entity
{
    public Guid TripId { get; private set; }

    public int Sequence { get; private set; }

    public Guid? LocationId { get; private set; }

    public string RawName { get; private set; }

    public Province Province { get; private set; }

    public DateTimeOffset? EstimatedArrivalUtc { get; private set; }

    public DateTimeOffset? EstimatedDepartureUtc { get; private set; }

    private TripStop()
    {
        RawName = string.Empty;
    }

    internal TripStop(Guid tripId, int sequence, Guid? locationId, string rawName, Province province)
    {
        TripId = tripId;
        Sequence = sequence;
        LocationId = locationId;
        RawName = rawName;
        Province = province;
    }
}
