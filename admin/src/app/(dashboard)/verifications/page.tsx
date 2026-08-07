import { getVerifications, type AdminVerification } from "@/lib/admin-api";
import { formatDateTime } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { FilterBar } from "@/components/FilterBar";
import { Pagination } from "@/components/Pagination";
import { Table, type Column } from "@/components/Table";
import { StatusBadge } from "@/components/StatusBadge";
import { VerificationActions } from "./VerificationActions";

const STATUS_OPTIONS = [
  { value: "Pending", label: "Pending" },
  { value: "Verified", label: "Verified" },
  { value: "Rejected", label: "Rejected" },
];

export default async function VerificationsPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; page?: string }>;
}) {
  const { status, page: pageParam } = await searchParams;
  const page = Number(pageParam ?? "1") || 1;

  const result = await getVerifications({ status, page, pageSize: 20 });

  const columns: Column<AdminVerification>[] = [
    {
      header: "Subject",
      cell: (v) => (
        <div>
          <p className="font-medium">{v.subjectDisplayName ?? "Unknown"}</p>
          <p className="text-xs text-text-secondary">{v.subjectType}</p>
        </div>
      ),
    },
    { header: "Document type", cell: (v) => v.type },
    { header: "Status", cell: (v) => <StatusBadge status={v.status} /> },
    { header: "Submitted", cell: (v) => (v.submittedAtUtc ? formatDateTime(v.submittedAtUtc) : "—") },
    {
      header: "Document",
      cell: (v) =>
        v.documentUrl ? (
          <a href={v.documentUrl} target="_blank" rel="noreferrer" className="text-accent hover:underline">
            View
          </a>
        ) : (
          "—"
        ),
    },
    {
      header: "",
      cell: (v) => (v.status === "Pending" ? <VerificationActions verificationId={v.id} /> : v.rejectionReason ?? null),
      className: "text-right",
    },
  ];

  return (
    <div>
      <PageHeader title="Verifications" description="Review driver-license and vehicle-registration submissions." />
      <FilterBar statusOptions={STATUS_OPTIONS} />
      <Table columns={columns} rows={result.items} rowKey={(v) => v.id} emptyMessage="Nothing in the queue." />
      <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} searchParams={{ status, page: pageParam }} />
    </div>
  );
}
