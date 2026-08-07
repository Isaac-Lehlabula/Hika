using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.RideAlerts.Dtos;
using Hika.Domain.RideAlerts;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.RideAlerts;

public sealed class RideAlertService(IAppDbContext db) : IRideAlertService
{
    public async Task<RideAlertResponse> CreateAsync(Guid userId, CreateRideAlertRequest request, CancellationToken cancellationToken)
    {
        var alert = RideAlert.Create(userId, request.Origin.Trim(), request.Destination.Trim(), request.TravelDate);
        db.RideAlerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(alert);
    }

    public async Task<IReadOnlyList<RideAlertResponse>> GetMyAlertsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var alerts = await db.RideAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return alerts.Select(ToResponse).ToList();
    }

    public async Task DeleteAsync(Guid userId, Guid alertId, CancellationToken cancellationToken)
    {
        var alert = await db.RideAlerts.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken)
            ?? throw new NotFoundException(nameof(RideAlert), alertId);

        if (alert.UserId != userId)
        {
            throw new NotFoundException(nameof(RideAlert), alertId);
        }

        db.RideAlerts.Remove(alert);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static RideAlertResponse ToResponse(RideAlert alert) => new()
    {
        Id = alert.Id,
        OriginRawText = alert.OriginRawText,
        DestinationRawText = alert.DestinationRawText,
        TravelDate = alert.TravelDate,
        Status = alert.Status.ToString(),
        CreatedAtUtc = alert.CreatedAtUtc,
    };
}
