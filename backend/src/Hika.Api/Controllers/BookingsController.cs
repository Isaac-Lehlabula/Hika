using Hika.Api.Common;
using Hika.Application.Bookings;
using Hika.Application.Bookings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/bookings")]
[Authorize]
public sealed class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> RequestBooking(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await bookingService.RequestAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetBooking), new { bookingId = booking.Id }, booking);
    }

    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetMyBookings(CancellationToken cancellationToken)
    {
        var bookings = await bookingService.GetMyBookingsAsync(User.GetUserId(), cancellationToken);
        return Ok(bookings);
    }

    [HttpGet("{bookingId:guid}")]
    public async Task<ActionResult<BookingResponse>> GetBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await bookingService.GetAsync(User.GetUserId(), bookingId, cancellationToken);
        return Ok(booking);
    }

    [HttpPost("{bookingId:guid}/cancel")]
    public async Task<ActionResult<BookingResponse>> CancelBooking(
        Guid bookingId, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await bookingService.CancelAsync(User.GetUserId(), bookingId, request.Reason, cancellationToken);
        return Ok(booking);
    }

    [HttpGet("trips/{tripId:guid}/requests")]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetTripRequests(Guid tripId, CancellationToken cancellationToken)
    {
        var bookings = await bookingService.GetTripRequestsAsync(User.GetUserId(), tripId, cancellationToken);
        return Ok(bookings);
    }

    [HttpPost("{bookingId:guid}/accept")]
    public async Task<ActionResult<BookingResponse>> AcceptBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await bookingService.AcceptAsync(User.GetUserId(), bookingId, cancellationToken);
        return Ok(booking);
    }

    [HttpPost("{bookingId:guid}/decline")]
    public async Task<ActionResult<BookingResponse>> DeclineBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await bookingService.DeclineAsync(User.GetUserId(), bookingId, cancellationToken);
        return Ok(booking);
    }

    [HttpPost("{bookingId:guid}/complete")]
    public async Task<ActionResult<BookingResponse>> CompleteBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await bookingService.CompleteAsync(User.GetUserId(), bookingId, cancellationToken);
        return Ok(booking);
    }
}
