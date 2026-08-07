using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Common.Storage;
using Hika.Application.Drivers.Dtos;
using Hika.Domain.Drivers;
using Hika.Domain.TrustSafety;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Drivers;

public sealed class VehicleService(IAppDbContext db, IFileStorage fileStorage) : IVehicleService
{
    public async Task<VehicleResponse> CreateAsync(Guid driverUserId, VehicleRequest request, CancellationToken cancellationToken)
    {
        var driverExists = await db.DriverProfiles.AnyAsync(p => p.Id == driverUserId, cancellationToken);
        if (!driverExists)
        {
            throw new AppValidationException("driverProfile", "Create a driver profile before adding a vehicle.");
        }

        var vehicle = Vehicle.Create(
            driverUserId, request.Make, request.Model, request.Year, request.Color,
            request.RegistrationNumber, request.SeatCapacity);

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(vehicle);
    }

    public async Task<IReadOnlyList<VehicleResponse>> ListOwnAsync(Guid driverUserId, CancellationToken cancellationToken)
    {
        var vehicles = await db.Vehicles
            .Include(v => v.Photos)
            .Where(v => v.DriverProfileId == driverUserId)
            .OrderBy(v => v.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return vehicles.Select(ToResponse).ToList();
    }

    public async Task<VehicleResponse> GetOwnAsync(Guid driverUserId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await FindOwnedAsync(driverUserId, vehicleId, cancellationToken);
        return ToResponse(vehicle);
    }

    public async Task<VehicleResponse> UpdateAsync(
        Guid driverUserId, Guid vehicleId, VehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await FindOwnedAsync(driverUserId, vehicleId, cancellationToken);

        vehicle.Update(request.Make, request.Model, request.Year, request.Color, request.RegistrationNumber, request.SeatCapacity);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(vehicle);
    }

    public async Task DeleteAsync(Guid driverUserId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await FindOwnedAsync(driverUserId, vehicleId, cancellationToken);

        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<VehiclePhotoResponse> UploadPhotoAsync(
        Guid driverUserId, Guid vehicleId, Stream content, string fileName, string contentType, bool isPrimary,
        CancellationToken cancellationToken)
    {
        var vehicle = await FindOwnedAsync(driverUserId, vehicleId, cancellationToken);

        var url = await fileStorage.SaveAsync(content, "vehicle-photos", fileName, contentType, cancellationToken);
        var photo = vehicle.AddPhoto(url, isPrimary);

        await db.SaveChangesAsync(cancellationToken);

        return new VehiclePhotoResponse { Id = photo.Id, Url = photo.Url, IsPrimary = photo.IsPrimary };
    }

    public async Task SubmitVehicleVerificationAsync(
        Guid driverUserId, Guid vehicleId, Stream content, string fileName, string contentType,
        CancellationToken cancellationToken)
    {
        var vehicle = await FindOwnedAsync(driverUserId, vehicleId, cancellationToken);

        var url = await fileStorage.SaveAsync(content, "verification-documents", fileName, contentType, cancellationToken);

        var verification = await db.Verifications
            .Where(v => v.SubjectType == VerificationSubjectType.Vehicle
                && v.SubjectId == vehicleId
                && v.Type == VerificationType.VehicleRegistration)
            .FirstOrDefaultAsync(cancellationToken);

        if (verification is null)
        {
            verification = Verification.CreateAndSubmit(VerificationSubjectType.Vehicle, vehicleId, VerificationType.VehicleRegistration, url);
            db.Verifications.Add(verification);
        }
        else
        {
            verification.Submit(url);
        }

        vehicle.MarkUnverified();

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Returns NotFound (never Forbidden) when a vehicle exists but belongs to someone else —
    /// doesn't reveal that the ID is valid to a caller who shouldn't be able to see it.
    /// </summary>
    private async Task<Vehicle> FindOwnedAsync(Guid driverUserId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await db.Vehicles
            .Include(v => v.Photos)
            .FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken);

        if (vehicle is null || vehicle.DriverProfileId != driverUserId)
        {
            throw new NotFoundException(nameof(Vehicle), vehicleId);
        }

        return vehicle;
    }

    private static VehicleResponse ToResponse(Vehicle vehicle) => new()
    {
        Id = vehicle.Id,
        Make = vehicle.Make,
        Model = vehicle.Model,
        Year = vehicle.Year,
        Color = vehicle.Color,
        RegistrationNumber = vehicle.RegistrationNumber,
        SeatCapacity = vehicle.SeatCapacity,
        IsVerified = vehicle.IsVerified,
        Photos = vehicle.Photos
            .OrderBy(p => p.SortOrder)
            .Select(p => new VehiclePhotoResponse { Id = p.Id, Url = p.Url, IsPrimary = p.IsPrimary })
            .ToList(),
    };
}
