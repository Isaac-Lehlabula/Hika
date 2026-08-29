using Hika.Application.Bookings.Dtos;
using Hika.Application.RideRequests.Dtos;

namespace Hika.Application.RideRequests;

public interface IRideRequestService
{
    Task<RideRequestResponse> CreateAsync(Guid riderUserId, CreateRideRequestRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<RideRequestResponse>> GetMyRequestsAsync(Guid riderUserId, CancellationToken cancellationToken);

    /// <summary>Every request that's still Open and whose TravelDate hasn't passed — the "demand
    /// board" drivers browse. No text filter yet (see RideRequestService's remarks); at this
    /// product's scale a plain list is enough for now.</summary>
    Task<IReadOnlyList<RideRequestResponse>> GetOpenRequestsAsync(CancellationToken cancellationToken);

    Task CancelAsync(Guid riderUserId, Guid requestId, CancellationToken cancellationToken);

    /// <summary>The accept-equivalent for this path — see RideRequest's remarks. Drives the same
    /// Booking.Request→Accept sequence a normal search-and-request does, so payment, chat,
    /// reviews and payout all just work.</summary>
    Task<BookingResponse> ClaimAsync(Guid driverUserId, Guid requestId, ClaimRideRequestRequest request, CancellationToken cancellationToken);
}
