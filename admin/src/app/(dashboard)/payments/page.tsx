import { getPayments, type AdminPayment } from "@/lib/admin-api";
import { formatCurrency, formatDateTime } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { FilterBar } from "@/components/FilterBar";
import { Pagination } from "@/components/Pagination";
import { Table, type Column } from "@/components/Table";
import { StatusBadge } from "@/components/StatusBadge";
import { ReasonDialog } from "@/components/ReasonDialog";
import { refundPaymentAction } from "./actions";

const STATUS_OPTIONS = [
  { value: "Pending", label: "Pending" },
  { value: "Succeeded", label: "Succeeded" },
  { value: "Failed", label: "Failed" },
  { value: "Refunded", label: "Refunded" },
];

export default async function PaymentsPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; page?: string }>;
}) {
  const { status, page: pageParam } = await searchParams;
  const page = Number(pageParam ?? "1") || 1;

  const result = await getPayments({ status, page, pageSize: 20 });

  const columns: Column<AdminPayment>[] = [
    { header: "Reference", cell: (p) => p.providerReference ?? "—" },
    { header: "Amount", cell: (p) => formatCurrency(p.amount) },
    { header: "Platform fee", cell: (p) => formatCurrency(p.platformFee) },
    { header: "Driver payout", cell: (p) => formatCurrency(p.driverPayoutAmount) },
    { header: "Status", cell: (p) => <StatusBadge status={p.status} /> },
    { header: "Created", cell: (p) => formatDateTime(p.createdAtUtc) },
    {
      header: "",
      cell: (p) =>
        p.status === "Succeeded" ? (
          <div className="text-right">
            <ReasonDialog
              action={refundPaymentAction.bind(null, p.bookingId)}
              fieldName="reason"
              title="Refund this payment"
              confirmLabel="Refund"
              variant="danger"
              triggerLabel="Refund"
            />
          </div>
        ) : null,
      className: "text-right",
    },
  ];

  return (
    <div>
      <PageHeader title="Payments" description="Financial oversight and admin-initiated refunds." />
      <FilterBar statusOptions={STATUS_OPTIONS} />
      <Table columns={columns} rows={result.items} rowKey={(p) => p.id} emptyMessage="No payments match that filter." />
      <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} searchParams={{ status, page: pageParam }} />
    </div>
  );
}
