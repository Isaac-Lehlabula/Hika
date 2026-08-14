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

    /// <summary>Applies a payment outcome to an AwaitingPayment booking — called inline by
    /// AcceptAsync when the gateway settles synchronously (Mock), and by
    /// OzowWebhooksController when it arrives later over a notify webhook. A no-op (returns the
    /// booking as-is) if the booking has already moved past AwaitingPayment, so a retried
    /// webhook delivery can't double-apply a state change.</summary>
    Task<BookingResponse> ResolvePaymentOutcomeAsync(
        Guid bookingId, bool succeeded, string? providerReference, CancellationToken cancellationToken);
}
