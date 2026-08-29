using Hika.Application.Notifications.Dtos;

namespace Hika.Application.Notifications;

public interface IDeviceTokenService
{
    Task RegisterAsync(Guid userId, RegisterDeviceTokenRequest request, CancellationToken cancellationToken);

    /// <summary>A no-op if the token doesn't exist or belongs to a different user — called
    /// best-effort on logout, not something worth surfacing an error for either way.</summary>
    Task UnregisterAsync(Guid userId, string token, CancellationToken cancellationToken);
}
