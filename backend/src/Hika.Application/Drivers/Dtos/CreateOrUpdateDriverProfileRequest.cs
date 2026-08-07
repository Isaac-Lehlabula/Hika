namespace Hika.Application.Drivers.Dtos;

public sealed record CreateOrUpdateDriverProfileRequest
{
    public required string LicenseNumber { get; init; }

    public required DateOnly LicenseExpiryDate { get; init; }
}
