namespace Hika.Application.Admin.Dtos;

public sealed record AdminBookingSummaryResponse
{
    public required Guid Id { get; init; }

    public required Guid TripId { get; init; }

    public required string PassengerName { get; init; }

    public required string DriverName { get; init; }

    public required string Status { get; init; }

    public required int SeatsRequested { get; init; }

    public required decimal TotalPrice { get; init; }

    public required DateTimeOffset RequestedAtUtc { get; init; }
}
