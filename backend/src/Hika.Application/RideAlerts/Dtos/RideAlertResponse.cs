namespace Hika.Application.RideAlerts.Dtos;

public sealed record RideAlertResponse
{
    public required Guid Id { get; init; }

    public required string OriginRawText { get; init; }

    public required string DestinationRawText { get; init; }

    public DateOnly? TravelDate { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
