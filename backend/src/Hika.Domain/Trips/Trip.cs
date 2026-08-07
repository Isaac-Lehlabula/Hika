using Hika.Domain.Common;

namespace Hika.Domain.Trips;

public enum TripStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
}

/// <summary>Input for one stop when creating a Trip — see Trip.Create.</summary>
public sealed record TripStopInput(Guid? LocationId, string RawName, Province Province);

/// <summary>
/// A driver's posted journey. Origin/destination are not separate columns — they are simply
/// Stops[0] and Stops[^1] (see docs/domain-model.md §4). Stops and Segments are created
/// together, atomically, in Create — a Trip can never exist with stops but no segments or
/// vice versa.
/// </summary>
public sealed class Trip : AuditableEntity
{
    public Guid DriverProfileId { get; private set; }

    public Guid VehicleId { get; private set; }

    public DateTimeOffset DepartureAtUtc { get; private set; }

    public int TotalSeatsOffered { get; private set; }

    public Money PricePerSeat { get; private set; }

    public string? LuggageAllowance { get; private set; }

    public string? Notes { get; private set; }

    public TripStatus Status { get; private set; }

    private readonly List<TripStop> _stops = [];

    /// <summary>Always in stop order — Stops[0] is the origin, Stops[^1] the destination.</summary>
    public IReadOnlyList<TripStop> Stops => [.. _stops.OrderBy(s => s.Sequence)];

    private readonly List<TripSegment> _segments = [];
    public IReadOnlyCollection<TripSegment> Segments => _segments.AsReadOnly();

    private Trip()
    {
        PricePerSeat = Money.Zero();
    }

    public static Trip Create(
        Guid driverProfileId,
        Guid vehicleId,
        DateTimeOffset departureAtUtc,
        int totalSeatsOffered,
        Money pricePerSeat,
        string? luggageAllowance,
        string? notes,
        IReadOnlyList<TripStopInput> stopsInOrder)
    {
        if (stopsInOrder.Count < 2)
        {
            throw new ArgumentException("A trip needs at least an origin and a destination stop.", nameof(stopsInOrder));
        }

        if (totalSeatsOffered < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSeatsOffered), totalSeatsOffered, "Must offer at least one seat.");
        }

        var trip = new Trip
        {
            DriverProfileId = driverProfileId,
            VehicleId = vehicleId,
            DepartureAtUtc = departureAtUtc,
            TotalSeatsOffered = totalSeatsOffered,
            PricePerSeat = pricePerSeat,
            LuggageAllowance = luggageAllowance,
            Notes = notes,
            Status = TripStatus.Scheduled,
        };

        var stops = new List<TripStop>();
        for (var i = 0; i < stopsInOrder.Count; i++)
        {
            var input = stopsInOrder[i];
            var stop = new TripStop(trip.Id, i, input.LocationId, input.RawName, input.Province);
            stops.Add(stop);
            trip._stops.Add(stop);
        }

        for (var i = 0; i < stops.Count - 1; i++)
        {
            trip._segments.Add(new TripSegment(trip.Id, stops[i].Id, stops[i + 1].Id, totalSeatsOffered));
        }

        return trip;
    }

    public void Cancel()
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel a trip that is already {Status}.");
        }

        Status = TripStatus.Cancelled;
    }
}
