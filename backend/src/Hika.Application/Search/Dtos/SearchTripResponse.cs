using Hika.Application.Trips.Dtos;

namespace Hika.Application.Search.Dtos;

/// <summary>
/// One search result. Boarding/alighting describe the rider's requested sub-range of the trip
/// (e.g. "Midrand" -> "Polokwane" out of a Johannesburg -> Giyani trip), not necessarily the
/// trip's full origin/destination — see docs/domain-model.md §4's segment-booking design.
/// </summary>
public sealed record SearchTripResponse
{
    public required Guid Id { get; init; }

    public required DateTimeOffset DepartureAtUtc { get; init; }

    public required string BoardingStopName { get; init; }

    public required string BoardingProvince { get; init; }

    public required string AlightingStopName { get; init; }

    public required string AlightingProvince { get; init; }

    public required int TotalSeatsOffered { get; init; }

    /// <summary>Seats available across the specific boarding-to-alighting range, not the whole trip.</summary>
    public required int SeatsAvailable { get; init; }

    public required decimal PricePerSeat { get; init; }

    public required TripDriverSummary Driver { get; init; }
}
