using Hika.Domain.Notifications;

namespace Hika.Application.Notifications.Dtos;

public sealed record RegisterDeviceTokenRequest
{
    public required string Token { get; init; }

    public required DevicePlatform Platform { get; init; }
}
