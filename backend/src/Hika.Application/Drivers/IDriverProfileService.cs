using Hika.Application.Drivers.Dtos;

namespace Hika.Application.Drivers;

public interface IDriverProfileService
{
    Task<DriverProfileResponse> CreateOrUpdateAsync(Guid userId, CreateOrUpdateDriverProfileRequest request, CancellationToken cancellationToken);

    Task<DriverProfileResponse> GetOwnAsync(Guid userId, CancellationToken cancellationToken);

    Task SubmitLicenseVerificationAsync(
        Guid userId, Stream content, string fileName, string contentType, CancellationToken cancellationToken);
}
