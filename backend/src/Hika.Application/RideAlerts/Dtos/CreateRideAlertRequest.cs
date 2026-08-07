namespace Hika.Application.RideAlerts.Dtos;

public sealed record CreateRideAlertRequest
{
    public required string Origin { get; init; }

    public required string Destination { get; init; }

    public DateOnly? TravelDate { get; init; }
}
