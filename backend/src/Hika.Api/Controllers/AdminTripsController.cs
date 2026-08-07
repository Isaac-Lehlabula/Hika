using Hika.Api.Common;
using Hika.Application.Admin;
using Hika.Application.Admin.Dtos;
using Hika.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/trips")]
[Authorize(Policy = "Admin")]
public sealed class AdminTripsController(IAdminTripService adminTripService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTrips(
        [FromQuery] TripStatus? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await adminTripService.GetTripsAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{tripId:guid}/remove")]
    public async Task<ActionResult<AdminTripSummaryResponse>> RemoveTrip(
        Guid tripId, RemoveTripRequest request, CancellationToken cancellationToken)
    {
        var trip = await adminTripService.RemoveAsync(User.GetUserId(), tripId, request.Reason, cancellationToken);
        return Ok(trip);
    }
}
