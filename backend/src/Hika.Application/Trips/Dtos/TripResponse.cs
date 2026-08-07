namespace Hika.Application.Trips.Dtos;

public sealed record TripStopResponse
{
    public required int Sequence { get; init; }

    public Guid? LocationId { get; init; }

    public required string Name { get; init; }

    public required string Province { get; init; }
}

public sealed record TripSegmentResponse
{
    public required int FromSequence { get; init; }

    public required int ToSequence { get; init; }

    public required int SeatsAvailable { get; init; }
}

public sealed record TripDriverSummary
{
    public required Guid UserId { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? PhotoUrl { get; init; }

    public decimal? AverageRating { get; init; }

    public required int CompletedTripCount { get; init; }

    public required bool IsVerifiedDriver { get; init; }
}

public sealed record TripVehicleSummary
{
    public required Guid Id { get; init; }

    public required string Make { get; init; }

    public required string Model { get; init; }

    public required int Year { get; init; }

    public required string Color { get; init; }

    public required bool IsVerified { get; init; }

    public string? PrimaryPhotoUrl { get; init; }
}

public sealed record TripResponse
{
    public required Guid Id { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset DepartureAtUtc { get; init; }

    public required int TotalSeatsOffered { get; init; }

    public required decimal PricePerSeat { get; init; }

    public string? LuggageAllowance { get; init; }

    public string? Notes { get; init; }

    public required TripDriverSummary Driver { get; init; }

    public required TripVehicleSummary Vehicle { get; init; }

    public required IReadOnlyList<TripStopResponse> Stops { get; init; }

    public required IReadOnlyList<TripSegmentResponse> Segments { get; init; }
}

/// <summary>Lighter-weight shape for list endpoints (my trips, search results).</summary>
public sealed record TripSummaryResponse
{
    public required Guid Id { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset DepartureAtUtc { get; init; }

    public required string OriginName { get; init; }

    public required string DestinationName { get; init; }

    public required int TotalSeatsOffered { get; init; }

    public required int MinSeatsAvailable { get; init; }

    public required decimal PricePerSeat { get; init; }

    public required TripDriverSummary Driver { get; init; }
}
