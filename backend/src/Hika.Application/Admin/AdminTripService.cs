using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Application.Trips;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminTripService(IAppDbContext db, IAuditLogger auditLogger) : IAdminTripService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<AdminTripSummaryResponse>> GetTripsAsync(
        TripStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.Trips.Include(t => t.Stops).AsQueryable();
        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        query = query.OrderByDescending(t => t.DepartureAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var trips = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var responses = await BuildResponsesAsync(trips, cancellationToken);
        return PagedResult<AdminTripSummaryResponse>.Create(responses, page, pageSize, totalCount);
    }

    public async Task<AdminTripSummaryResponse> RemoveAsync(
        Guid adminUserId, Guid tripId, string reason, CancellationToken cancellationToken)
    {
        var trip = await db.Trips.Include(t => t.Stops).FirstOrDefaultAsync(t => t.Id == tripId, cancellationToken)
            ?? throw new NotFoundException(nameof(Trip), tripId);

        trip.Cancel();
        auditLogger.Record(adminUserId, "RemoveTrip", nameof(Trip), tripId, reason);
        await db.SaveChangesAsync(cancellationToken);

        return (await BuildResponsesAsync([trip], cancellationToken))[0];
    }

    private async Task<IReadOnlyList<AdminTripSummaryResponse>> BuildResponsesAsync(
        IReadOnlyList<Trip> trips, CancellationToken cancellationToken)
    {
        if (trips.Count == 0)
        {
            return [];
        }

        var driverIds = trips.Select(t => t.DriverProfileId).Distinct().ToList();
        var driverNames = await db.UserProfiles
            .Where(p => driverIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName}", cancellationToken);

        var locations = await TripDisplayHelpers.LoadLocationsAsync(db, trips.SelectMany(t => t.Stops), cancellationToken);

        return trips.Select(t => new AdminTripSummaryResponse
        {
            Id = t.Id,
            DriverName = driverNames.GetValueOrDefault(t.DriverProfileId, "Unknown"),
            DriverUserId = t.DriverProfileId,
            OriginName = TripDisplayHelpers.ResolveStopName(t.Stops[0], locations),
            DestinationName = TripDisplayHelpers.ResolveStopName(t.Stops[^1], locations),
            DepartureAtUtc = t.DepartureAtUtc,
            Status = t.Status.ToString(),
            TotalSeatsOffered = t.TotalSeatsOffered,
            PricePerSeat = t.PricePerSeat.Amount,
        }).ToList();
    }
}
