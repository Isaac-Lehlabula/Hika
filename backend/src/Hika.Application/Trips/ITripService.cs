using Hika.Application.Trips.Dtos;

namespace Hika.Application.Trips;

public interface ITripService
{
    Task<TripResponse> CreateAsync(Guid driverUserId, CreateTripRequest request, CancellationToken cancellationToken);

    Task<TripResponse> GetAsync(Guid tripId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TripSummaryResponse>> GetMyTripsAsync(Guid driverUserId, CancellationToken cancellationToken);

    Task CancelAsync(Guid driverUserId, Guid tripId, CancellationToken cancellationToken);
}
