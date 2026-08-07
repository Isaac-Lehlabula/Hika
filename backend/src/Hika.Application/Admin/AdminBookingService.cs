using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminBookingService(IAppDbContext db) : IAdminBookingService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<AdminBookingSummaryResponse>> GetBookingsAsync(
        BookingStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.Bookings.AsQueryable();
        if (status is not null)
        {
            query = query.Where(b => b.Status == status);
        }

        query = query.OrderByDescending(b => b.RequestedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var bookings = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        if (bookings.Count == 0)
        {
            return PagedResult<AdminBookingSummaryResponse>.Create([], page, pageSize, totalCount);
        }

        var tripIds = bookings.Select(b => b.TripId).Distinct().ToList();
        var trips = await db.Trips.Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);

        var passengerIds = bookings.Select(b => b.PassengerUserId).Distinct().ToList();
        var driverIds = trips.Values.Select(t => t.DriverProfileId).Distinct().ToList();
        var names = await db.UserProfiles
            .Where(p => passengerIds.Contains(p.Id) || driverIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName}", cancellationToken);

        var responses = bookings.Select(b =>
        {
            var driverName = trips.TryGetValue(b.TripId, out var trip) ? names.GetValueOrDefault(trip.DriverProfileId, "Unknown") : "Unknown";
            return new AdminBookingSummaryResponse
            {
                Id = b.Id,
                TripId = b.TripId,
                PassengerName = names.GetValueOrDefault(b.PassengerUserId, "Unknown"),
                DriverName = driverName,
                Status = b.Status.ToString(),
                SeatsRequested = b.SeatsRequested,
                TotalPrice = b.TotalPrice.Amount,
                RequestedAtUtc = b.RequestedAtUtc,
            };
        }).ToList();

        return PagedResult<AdminBookingSummaryResponse>.Create(responses, page, pageSize, totalCount);
    }
}
