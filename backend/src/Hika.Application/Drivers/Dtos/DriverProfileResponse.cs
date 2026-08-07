namespace Hika.Application.Drivers.Dtos;

public sealed record DriverProfileResponse
{
    public required Guid UserId { get; init; }

    public required string LicenseNumber { get; init; }

    public required DateOnly LicenseExpiryDate { get; init; }

    public required bool IsVerifiedDriver { get; init; }

    public required string VerificationStatus { get; init; }
}
