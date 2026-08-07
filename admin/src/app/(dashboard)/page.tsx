import { getAnalyticsOverview } from "@/lib/admin-api";
import { formatCurrency } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";

function StatTile({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="rounded-xl border border-border bg-surface p-5">
      <p className="text-sm text-text-secondary">{label}</p>
      <p className="mt-2 text-2xl font-semibold">{value}</p>
      {hint && <p className="mt-1 text-xs text-text-secondary">{hint}</p>}
    </div>
  );
}

export default async function DashboardPage() {
  const overview = await getAnalyticsOverview();

  return (
    <div>
      <PageHeader title="Dashboard" description="Platform activity at a glance." />

      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
        <StatTile label="Total users" value={overview.totalUsers.toLocaleString()} hint={`${overview.totalDrivers} drivers`} />
        <StatTile
          label="Active last 30 days"
          value={overview.activeUsersLast30Days.toLocaleString()}
          hint={`${overview.tripsPostedLast30Days} trips posted`}
        />
        <StatTile
          label="Total trips"
          value={overview.totalTrips.toLocaleString()}
          hint={`${overview.scheduledTrips} scheduled`}
        />
        <StatTile
          label="Total bookings"
          value={overview.totalBookings.toLocaleString()}
          hint={`${overview.completedBookings} completed`}
        />
        <StatTile label="Gross merchandise value" value={formatCurrency(overview.grossMerchandiseValue)} />
        <StatTile label="Platform fees earned" value={formatCurrency(overview.totalPlatformFees)} />
        <StatTile label="Open reports" value={overview.openReports.toLocaleString()} hint="Needs review" />
        <StatTile label="Pending verifications" value={overview.pendingVerifications.toLocaleString()} hint="Needs review" />
      </div>
    </div>
  );
}
