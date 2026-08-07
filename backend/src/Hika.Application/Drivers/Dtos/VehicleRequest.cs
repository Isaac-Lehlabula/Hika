namespace Hika.Application.Drivers.Dtos;

public sealed record VehicleRequest
{
    public required string Make { get; init; }

    public required string Model { get; init; }

    public required int Year { get; init; }

    public required string Color { get; init; }

    public required string RegistrationNumber { get; init; }

    public required int SeatCapacity { get; init; }
}
