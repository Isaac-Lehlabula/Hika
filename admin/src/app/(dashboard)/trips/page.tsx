import { getTrips, type AdminTrip } from "@/lib/admin-api";
import { formatCurrency, formatDateTime } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { FilterBar } from "@/components/FilterBar";
import { Pagination } from "@/components/Pagination";
import { Table, type Column } from "@/components/Table";
import { StatusBadge } from "@/components/StatusBadge";
import { ReasonDialog } from "@/components/ReasonDialog";
import { removeTripAction } from "./actions";

const STATUS_OPTIONS = [
  { value: "Scheduled", label: "Scheduled" },
  { value: "InProgress", label: "In progress" },
  { value: "Completed", label: "Completed" },
  { value: "Cancelled", label: "Cancelled" },
];

export default async function TripsPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; page?: string }>;
}) {
  const { status, page: pageParam } = await searchParams;
  const page = Number(pageParam ?? "1") || 1;

  const result = await getTrips({ status, page, pageSize: 20 });

  const columns: Column<AdminTrip>[] = [
    {
      header: "Route",
      cell: (trip) => (
        <div>
          <p className="font-medium">{trip.originName} → {trip.destinationName}</p>
          <p className="text-xs text-text-secondary">{formatDateTime(trip.departureAtUtc)}</p>
        </div>
      ),
    },
    { header: "Driver", cell: (trip) => trip.driverName },
    { header: "Status", cell: (trip) => <StatusBadge status={trip.status} /> },
    { header: "Seats", cell: (trip) => trip.totalSeatsOffered },
    { header: "Price/seat", cell: (trip) => formatCurrency(trip.pricePerSeat) },
    {
      header: "",
      cell: (trip) =>
        trip.status === "Scheduled" ? (
          <div className="text-right">
            <ReasonDialog
              action={removeTripAction.bind(null, trip.id)}
              fieldName="reason"
              title="Remove this trip"
              description="It will be cancelled immediately."
              confirmLabel="Remove"
              variant="danger"
              triggerLabel="Remove"
            />
          </div>
        ) : null,
      className: "text-right",
    },
  ];

  return (
    <div>
      <PageHeader title="Trips" description="Oversee posted trips and remove ones that violate policy." />
      <FilterBar statusOptions={STATUS_OPTIONS} />
      <Table columns={columns} rows={result.items} rowKey={(trip) => trip.id} emptyMessage="No trips match that filter." />
      <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} searchParams={{ status, page: pageParam }} />
    </div>
  );
}
