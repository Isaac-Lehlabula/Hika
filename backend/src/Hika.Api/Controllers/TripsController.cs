using Hika.Api.Common;
using Hika.Application.Trips;
using Hika.Application.Trips.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
public sealed class TripsController(ITripService tripService) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TripResponse>> CreateTrip(CreateTripRequest request, CancellationToken cancellationToken)
    {
        var trip = await tripService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetTrip), new { tripId = trip.Id }, trip);
    }

    [HttpGet("{tripId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TripResponse>> GetTrip(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await tripService.GetAsync(tripId, cancellationToken);
        return Ok(trip);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<TripSummaryResponse>>> GetMyTrips(CancellationToken cancellationToken)
    {
        var trips = await tripService.GetMyTripsAsync(User.GetUserId(), cancellationToken);
        return Ok(trips);
    }

    [HttpPost("{tripId:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> CancelTrip(Guid tripId, CancellationToken cancellationToken)
    {
        await tripService.CancelAsync(User.GetUserId(), tripId, cancellationToken);
        return NoContent();
    }
}
