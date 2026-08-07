import { getAuditLogs, type AuditLogEntry } from "@/lib/admin-api";
import { formatDateTime } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { Table, type Column } from "@/components/Table";

export default async function AuditLogPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string }>;
}) {
  const { page: pageParam } = await searchParams;
  const page = Number(pageParam ?? "1") || 1;

  const result = await getAuditLogs({ page, pageSize: 30 });

  const columns: Column<AuditLogEntry>[] = [
    { header: "When", cell: (log) => formatDateTime(log.createdAtUtc) },
    { header: "Admin", cell: (log) => log.adminName },
    { header: "Action", cell: (log) => <code className="text-xs">{log.action}</code> },
    { header: "Entity", cell: (log) => `${log.entityType}${log.entityId ? ` · ${log.entityId.slice(0, 8)}…` : ""}` },
    { header: "Details", cell: (log) => <span className="text-text-secondary">{log.details ?? "—"}</span> },
  ];

  return (
    <div>
      <PageHeader title="Audit log" description="Every sensitive admin action, in order." />
      <Table columns={columns} rows={result.items} rowKey={(log) => log.id} emptyMessage="No admin actions recorded yet." />
      <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} searchParams={{ page: pageParam }} />
    </div>
  );
}
