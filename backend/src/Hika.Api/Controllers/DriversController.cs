using Hika.Api.Common;
using Hika.Application.Drivers;
using Hika.Application.Drivers.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/drivers")]
[Authorize]
public sealed class DriversController(IDriverProfileService driverProfileService, IVehicleService vehicleService)
    : ControllerBase
{
    [HttpPost("me/driver-profile")]
    public async Task<ActionResult<DriverProfileResponse>> CreateOrUpdateDriverProfile(
        CreateOrUpdateDriverProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await driverProfileService.CreateOrUpdateAsync(User.GetUserId(), request, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("me/driver-profile")]
    public async Task<ActionResult<DriverProfileResponse>> GetOwnDriverProfile(CancellationToken cancellationToken)
    {
        var profile = await driverProfileService.GetOwnAsync(User.GetUserId(), cancellationToken);
        return Ok(profile);
    }

    [HttpPost("me/driver-profile/verification-documents")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> SubmitLicenseVerification(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        await driverProfileService.SubmitLicenseVerificationAsync(
            User.GetUserId(), stream, file.FileName, file.ContentType, cancellationToken);
        return Accepted();
    }

    [HttpPost("me/vehicles")]
    public async Task<ActionResult<VehicleResponse>> CreateVehicle(VehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
    }

    [HttpGet("me/vehicles")]
    public async Task<ActionResult<IReadOnlyList<VehicleResponse>>> ListVehicles(CancellationToken cancellationToken)
    {
        var vehicles = await vehicleService.ListOwnAsync(User.GetUserId(), cancellationToken);
        return Ok(vehicles);
    }

    [HttpGet("me/vehicles/{id:guid}")]
    public async Task<ActionResult<VehicleResponse>> GetVehicle(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleService.GetOwnAsync(User.GetUserId(), id, cancellationToken);
        return Ok(vehicle);
    }

    [HttpPut("me/vehicles/{id:guid}")]
    public async Task<ActionResult<VehicleResponse>> UpdateVehicle(Guid id, VehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleService.UpdateAsync(User.GetUserId(), id, request, cancellationToken);
        return Ok(vehicle);
    }

    [HttpDelete("me/vehicles/{id:guid}")]
    public async Task<IActionResult> DeleteVehicle(Guid id, CancellationToken cancellationToken)
    {
        await vehicleService.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpPost("me/vehicles/{id:guid}/photos")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<VehiclePhotoResponse>> UploadVehiclePhoto(
        Guid id, IFormFile file, [FromForm] bool isPrimary, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var photo = await vehicleService.UploadPhotoAsync(
            User.GetUserId(), id, stream, file.FileName, file.ContentType, isPrimary, cancellationToken);
        return Ok(photo);
    }

    [HttpPost("me/vehicles/{id:guid}/verification-documents")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> SubmitVehicleVerification(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        await vehicleService.SubmitVehicleVerificationAsync(
            User.GetUserId(), id, stream, file.FileName, file.ContentType, cancellationToken);
        return Accepted();
    }
}
