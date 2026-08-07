using Hika.Application.Admin;
using Hika.Domain.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/bookings")]
[Authorize(Policy = "Admin")]
public sealed class AdminBookingsController(IAdminBookingService adminBookingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetBookings(
        [FromQuery] BookingStatus? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await adminBookingService.GetBookingsAsync(status, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
