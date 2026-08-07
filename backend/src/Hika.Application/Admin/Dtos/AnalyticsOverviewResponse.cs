namespace Hika.Application.Admin.Dtos;

public sealed record AnalyticsOverviewResponse
{
    public required int TotalUsers { get; init; }

    public required int TotalDrivers { get; init; }

    public required int TotalTrips { get; init; }

    public required int ScheduledTrips { get; init; }

    public required int TotalBookings { get; init; }

    public required int CompletedBookings { get; init; }

    public required decimal GrossMerchandiseValue { get; init; }

    public required decimal TotalPlatformFees { get; init; }

    /// <summary>Distinct users with at least one Trip or Booking row created in the last 30 days —
    /// a cheap proxy for activity, not a tracked session/event log (none exists at MVP scale).</summary>
    public required int ActiveUsersLast30Days { get; init; }

    public required int TripsPostedLast30Days { get; init; }

    public required int OpenReports { get; init; }

    public required int PendingVerifications { get; init; }
}
