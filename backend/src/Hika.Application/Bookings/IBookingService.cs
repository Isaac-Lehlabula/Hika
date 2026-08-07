using Hika.Application.Bookings.Dtos;

namespace Hika.Application.Bookings;

public interface IBookingService
{
    Task<BookingResponse> RequestAsync(Guid passengerUserId, CreateBookingRequest request, CancellationToken cancellationToken);

    Task<BookingResponse> GetAsync(Guid callerId, Guid bookingId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BookingResponse>> GetMyBookingsAsync(Guid passengerUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BookingResponse>> GetTripRequestsAsync(Guid driverUserId, Guid tripId, CancellationToken cancellationToken);

    Task<BookingResponse> AcceptAsync(Guid driverUserId, Guid bookingId, CancellationToken cancellationToken);

    Task<BookingResponse> DeclineAsync(Guid driverUserId, Guid bookingId, CancellationToken cancellationToken);

    Task<BookingResponse> CancelAsync(Guid passengerUserId, Guid bookingId, string? reason, CancellationToken cancellationToken);

    Task<BookingResponse> CompleteAsync(Guid driverUserId, Guid bookingId, CancellationToken cancellationToken);
}
