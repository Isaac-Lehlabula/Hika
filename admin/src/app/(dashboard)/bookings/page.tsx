import { getBookings, type AdminBooking } from "@/lib/admin-api";
import { formatCurrency, formatDateTime } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { FilterBar } from "@/components/FilterBar";
import { Pagination } from "@/components/Pagination";
import { Table, type Column } from "@/components/Table";
import { StatusBadge } from "@/components/StatusBadge";

const STATUS_OPTIONS = [
  { value: "Pending", label: "Pending" },
  { value: "Confirmed", label: "Confirmed" },
  { value: "Declined", label: "Declined" },
  { value: "Cancelled", label: "Cancelled" },
  { value: "Completed", label: "Completed" },
];

export default async function BookingsPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; page?: string }>;
}) {
  const { status, page: pageParam } = await searchParams;
  const page = Number(pageParam ?? "1") || 1;

  const result = await getBookings({ status, page, pageSize: 20 });

  const columns: Column<AdminBooking>[] = [
    { header: "Passenger", cell: (b) => b.passengerName },
    { header: "Driver", cell: (b) => b.driverName },
    { header: "Status", cell: (b) => <StatusBadge status={b.status} /> },
    { header: "Seats", cell: (b) => b.seatsRequested },
    { header: "Total price", cell: (b) => formatCurrency(b.totalPrice) },
    { header: "Requested", cell: (b) => formatDateTime(b.requestedAtUtc) },
  ];

  return (
    <div>
      <PageHeader title="Bookings" description="Read-only view of booking activity across the platform." />
      <FilterBar statusOptions={STATUS_OPTIONS} />
      <Table columns={columns} rows={result.items} rowKey={(b) => b.id} emptyMessage="No bookings match that filter." />
      <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} searchParams={{ status, page: pageParam }} />
    </div>
  );
}
