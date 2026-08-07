namespace Hika.Application.Admin.Dtos;

public sealed record AdminTripSummaryResponse
{
    public required Guid Id { get; init; }

    public required string DriverName { get; init; }

    public required Guid DriverUserId { get; init; }

    public required string OriginName { get; init; }

    public required string DestinationName { get; init; }

    public required DateTimeOffset DepartureAtUtc { get; init; }

    public required string Status { get; init; }

    public required int TotalSeatsOffered { get; init; }

    public required decimal PricePerSeat { get; init; }
}
