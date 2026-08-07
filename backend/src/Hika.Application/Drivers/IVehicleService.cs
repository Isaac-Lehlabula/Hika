using Hika.Application.Drivers.Dtos;

namespace Hika.Application.Drivers;

public interface IVehicleService
{
    Task<VehicleResponse> CreateAsync(Guid driverUserId, VehicleRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<VehicleResponse>> ListOwnAsync(Guid driverUserId, CancellationToken cancellationToken);

    Task<VehicleResponse> GetOwnAsync(Guid driverUserId, Guid vehicleId, CancellationToken cancellationToken);

    Task<VehicleResponse> UpdateAsync(Guid driverUserId, Guid vehicleId, VehicleRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid driverUserId, Guid vehicleId, CancellationToken cancellationToken);

    Task<VehiclePhotoResponse> UploadPhotoAsync(
        Guid driverUserId, Guid vehicleId, Stream content, string fileName, string contentType, bool isPrimary,
        CancellationToken cancellationToken);

    Task SubmitVehicleVerificationAsync(
        Guid driverUserId, Guid vehicleId, Stream content, string fileName, string contentType,
        CancellationToken cancellationToken);
}
