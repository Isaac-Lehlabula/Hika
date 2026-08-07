using Hika.Domain.Common;

namespace Hika.Application.Trips.Dtos;

public sealed record CreateTripStopRequest
{
    public Guid? LocationId { get; init; }

    public required string RawName { get; init; }

    public required Province Province { get; init; }
}

public sealed record CreateTripRequest
{
    public required Guid VehicleId { get; init; }

    public required DateTimeOffset DepartureAtUtc { get; init; }

    public required int TotalSeatsOffered { get; init; }

    public required decimal PricePerSeat { get; init; }

    public string? LuggageAllowance { get; init; }

    public string? Notes { get; init; }

    /// <summary>Ordered origin-to-destination — see docs/domain-model.md §4.</summary>
    public required IReadOnlyList<CreateTripStopRequest> Stops { get; init; }
}
