namespace Hika.Application.Drivers.Dtos;

public sealed record VehicleResponse
{
    public required Guid Id { get; init; }

    public required string Make { get; init; }

    public required string Model { get; init; }

    public required int Year { get; init; }

    public required string Color { get; init; }

    public required string RegistrationNumber { get; init; }

    public required int SeatCapacity { get; init; }

    public required bool IsVerified { get; init; }

    public required IReadOnlyList<VehiclePhotoResponse> Photos { get; init; }
}

public sealed record VehiclePhotoResponse
{
    public required Guid Id { get; init; }

    public required string Url { get; init; }

    public required bool IsPrimary { get; init; }
}
