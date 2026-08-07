namespace Hika.Application.Bookings.Dtos;

public sealed record CancelBookingRequest
{
    public string? Reason { get; init; }
}
