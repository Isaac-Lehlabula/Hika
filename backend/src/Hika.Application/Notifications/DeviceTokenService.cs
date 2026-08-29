using Hika.Application.Common.Persistence;
using Hika.Application.Notifications.Dtos;
using Hika.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Notifications;

public sealed class DeviceTokenService(IAppDbContext db) : IDeviceTokenService
{
    public async Task RegisterAsync(Guid userId, RegisterDeviceTokenRequest request, CancellationToken cancellationToken)
    {
        var existing = await db.DeviceTokens.FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (existing is null)
        {
            db.DeviceTokens.Add(DeviceToken.Register(userId, request.Token, request.Platform));
        }
        else
        {
            // Same physical device, possibly a different (or the same) account logging in —
            // reassign rather than reject. See DeviceToken's remarks.
            existing.ReassignTo(userId);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnregisterAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        var existing = await db.DeviceTokens.FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId, cancellationToken);
        if (existing is null)
        {
            return;
        }

        db.DeviceTokens.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }
}
