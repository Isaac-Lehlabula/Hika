import { getUsers } from "@/lib/admin-api";
import { formatDate } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { FilterBar } from "@/components/FilterBar";
import { Pagination } from "@/components/Pagination";
import { Table, type Column } from "@/components/Table";
import { SuspendUserButton } from "./SuspendUserButton";
import type { AdminUser } from "@/lib/admin-api";

export default async function UsersPage({
  searchParams,
}: {
  searchParams: Promise<{ search?: string; page?: string }>;
}) {
  const { search, page: pageParam } = await searchParams;
  const page = Number(pageParam ?? "1") || 1;

  const result = await getUsers({ search, page, pageSize: 20 });

  const columns: Column<AdminUser>[] = [
    {
      header: "Name",
      cell: (user) => (
        <div>
          <p className="font-medium">{user.firstName} {user.lastName}</p>
          <p className="text-xs text-text-secondary">{user.email}</p>
        </div>
      ),
    },
    {
      header: "Status",
      cell: (user) => (
        <div className="flex flex-wrap gap-1.5">
          {user.isAdmin && <span className="rounded-full bg-accent-light px-2 py-0.5 text-xs font-medium text-accent">Admin</span>}
          {user.isSuspended ? (
            <span className="rounded-full bg-danger-light px-2 py-0.5 text-xs font-medium text-danger" title={user.suspensionReason ?? undefined}>
              Suspended
            </span>
          ) : (
            <span className="rounded-full bg-success-light px-2 py-0.5 text-xs font-medium text-success">Active</span>
          )}
        </div>
      ),
    },
    {
      header: "Verified",
      cell: (user) => (
        <span className="text-text-secondary">
          {user.emailVerified ? "Email ✓" : "Email ✗"} · {user.phoneVerified ? "Phone ✓" : "Phone ✗"}
        </span>
      ),
    },
    {
      header: "Rating",
      cell: (user) => (user.averageRating ? `★ ${user.averageRating.toFixed(1)} (${user.completedTripCount})` : "—"),
    },
    { header: "Member since", cell: (user) => formatDate(user.memberSinceUtc) },
    {
      header: "",
      cell: (user) => <SuspendUserButton userId={user.userId} isSuspended={user.isSuspended} />,
      className: "text-right",
    },
  ];

  return (
    <div>
      <PageHeader title="Users" description="Search, review, and suspend accounts." />
      <FilterBar searchPlaceholder="Search by name…" />
      <Table columns={columns} rows={result.items} rowKey={(user) => user.userId} emptyMessage="No users match that search." />
      <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} searchParams={{ search, page: pageParam }} />
    </div>
  );
}
