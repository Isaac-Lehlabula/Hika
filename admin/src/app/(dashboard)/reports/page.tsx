import { getReports, type AdminReport } from "@/lib/admin-api";
import { formatDateTime } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { FilterBar } from "@/components/FilterBar";
import { Pagination } from "@/components/Pagination";
import { Table, type Column } from "@/components/Table";
import { StatusBadge } from "@/components/StatusBadge";
import { ReportActions } from "./ReportActions";

const STATUS_OPTIONS = [
  { value: "Open", label: "Open" },
  { value: "UnderReview", label: "Under review" },
  { value: "Resolved", label: "Resolved" },
  { value: "Dismissed", label: "Dismissed" },
];

export default async function ReportsPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; page?: string }>;
}) {
  const { status, page: pageParam } = await searchParams;
  const page = Number(pageParam ?? "1") || 1;

  const result = await getReports({ status, page, pageSize: 20 });

  const columns: Column<AdminReport>[] = [
    {
      header: "Reported",
      cell: (r) => (
        <div>
          <p className="font-medium">{r.reportedUserName ?? (r.reportedTripId ? "A trip" : "Unknown")}</p>
          <p className="text-xs text-text-secondary">by {r.reporterName}</p>
        </div>
      ),
    },
    { header: "Reason", cell: (r) => r.reason },
    { header: "Description", cell: (r) => <span className="line-clamp-2 max-w-xs text-text-secondary">{r.description}</span> },
    { header: "Status", cell: (r) => <StatusBadge status={r.status} /> },
    { header: "Filed", cell: (r) => formatDateTime(r.createdAtUtc) },
    {
      header: "",
      cell: (r) => (r.status === "Open" || r.status === "UnderReview" ? <ReportActions reportId={r.id} /> : null),
      className: "text-right",
    },
  ];

  return (
    <div>
      <PageHeader title="Reports" description="Safety reports filed by users about other users or trips." />
      <FilterBar statusOptions={STATUS_OPTIONS} />
      <Table columns={columns} rows={result.items} rowKey={(r) => r.id} emptyMessage="No reports match that filter." />
      <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} searchParams={{ status, page: pageParam }} />
    </div>
  );
}
