using Hika.Application.RideAlerts.Dtos;

namespace Hika.Application.RideAlerts;

public interface IRideAlertService
{
    Task<RideAlertResponse> CreateAsync(Guid userId, CreateRideAlertRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<RideAlertResponse>> GetMyAlertsAsync(Guid userId, CancellationToken cancellationToken);

    Task DeleteAsync(Guid userId, Guid alertId, CancellationToken cancellationToken);
}
