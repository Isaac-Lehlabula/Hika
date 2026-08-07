using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Persistence;
using Hika.Domain.Bookings;
using Hika.Domain.Payments;
using Hika.Domain.Trips;
using Hika.Domain.TrustSafety;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminAnalyticsService(IAppDbContext db) : IAdminAnalyticsService
{
    public async Task<AnalyticsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

        var totalUsers = await db.UserProfiles.CountAsync(cancellationToken);
        var totalDrivers = await db.DriverProfiles.CountAsync(cancellationToken);
        var totalTrips = await db.Trips.CountAsync(cancellationToken);
        var scheduledTrips = await db.Trips.CountAsync(t => t.Status == TripStatus.Scheduled, cancellationToken);
        var totalBookings = await db.Bookings.CountAsync(cancellationToken);
        var completedBookings = await db.Bookings.CountAsync(b => b.Status == BookingStatus.Completed, cancellationToken);

        var succeededPayments = db.Payments.Where(p => p.Status == PaymentStatus.Succeeded);
        var gmv = await succeededPayments.SumAsync(p => (decimal?)p.Amount.Amount, cancellationToken) ?? 0m;
        var totalFees = await succeededPayments.SumAsync(p => (decimal?)p.PlatformFee.Amount, cancellationToken) ?? 0m;

        var recentDriverIds = await db.Trips.Where(t => t.CreatedAtUtc >= cutoff).Select(t => t.DriverProfileId).Distinct().ToListAsync(cancellationToken);
        var recentPassengerIds = await db.Bookings.Where(b => b.RequestedAtUtc >= cutoff).Select(b => b.PassengerUserId).Distinct().ToListAsync(cancellationToken);
        var activeUsers = recentDriverIds.Concat(recentPassengerIds).Distinct().Count();

        var tripsLast30Days = await db.Trips.CountAsync(t => t.CreatedAtUtc >= cutoff, cancellationToken);
        var openReports = await db.Reports.CountAsync(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.UnderReview, cancellationToken);
        var pendingVerifications = await db.Verifications.CountAsync(v => v.Status == VerificationStatus.Pending, cancellationToken);

        return new AnalyticsOverviewResponse
        {
            TotalUsers = totalUsers,
            TotalDrivers = totalDrivers,
            TotalTrips = totalTrips,
            ScheduledTrips = scheduledTrips,
            TotalBookings = totalBookings,
            CompletedBookings = completedBookings,
            GrossMerchandiseValue = gmv,
            TotalPlatformFees = totalFees,
            ActiveUsersLast30Days = activeUsers,
            TripsPostedLast30Days = tripsLast30Days,
            OpenReports = openReports,
            PendingVerifications = pendingVerifications,
        };
    }
}
