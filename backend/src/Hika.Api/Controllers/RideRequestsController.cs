using Hika.Api.Common;
using Hika.Application.Bookings.Dtos;
using Hika.Application.RideRequests;
using Hika.Application.RideRequests.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/ride-requests")]
[Authorize]
public sealed class RideRequestsController(IRideRequestService rideRequestService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RideRequestResponse>> CreateRequest(CreateRideRequestRequest request, CancellationToken cancellationToken)
    {
        var rideRequest = await rideRequestService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return Ok(rideRequest);
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<RideRequestResponse>>> GetMyRequests(CancellationToken cancellationToken)
    {
        var requests = await rideRequestService.GetMyRequestsAsync(User.GetUserId(), cancellationToken);
        return Ok(requests);
    }

    [HttpGet("open")]
    public async Task<ActionResult<IReadOnlyList<RideRequestResponse>>> GetOpenRequests(CancellationToken cancellationToken)
    {
        var requests = await rideRequestService.GetOpenRequestsAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpDelete("{requestId:guid}")]
    public async Task<IActionResult> CancelRequest(Guid requestId, CancellationToken cancellationToken)
    {
        await rideRequestService.CancelAsync(User.GetUserId(), requestId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{requestId:guid}/claim")]
    public async Task<ActionResult<BookingResponse>> ClaimRequest(
        Guid requestId, ClaimRideRequestRequest request, CancellationToken cancellationToken)
    {
        var booking = await rideRequestService.ClaimAsync(User.GetUserId(), requestId, request, cancellationToken);
        return Ok(booking);
    }
}
