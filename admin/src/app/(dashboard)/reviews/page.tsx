import { getReviews, type AdminReview } from "@/lib/admin-api";
import { formatDateTime } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { Pagination } from "@/components/Pagination";
import { Table, type Column } from "@/components/Table";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { deleteReviewAction } from "./actions";

export default async function ReviewsPage({
  searchParams,
}: {
  searchParams: Promise<{ page?: string }>;
}) {
  const { page: pageParam } = await searchParams;
  const page = Number(pageParam ?? "1") || 1;

  const result = await getReviews({ page, pageSize: 20 });

  const columns: Column<AdminReview>[] = [
    {
      header: "Review",
      cell: (r) => (
        <div>
          <p className="font-medium">{"★".repeat(r.rating)}{"☆".repeat(5 - r.rating)}</p>
          {r.comment && <p className="mt-0.5 max-w-sm text-text-secondary">{r.comment}</p>}
        </div>
      ),
    },
    { header: "From", cell: (r) => r.reviewerName },
    { header: "About", cell: (r) => r.revieweeName },
    { header: "Direction", cell: (r) => (r.direction === "PassengerToDriver" ? "Passenger → Driver" : "Driver → Passenger") },
    { header: "Posted", cell: (r) => formatDateTime(r.createdAtUtc) },
    {
      header: "",
      cell: (r) => (
        <div className="text-right">
          <ConfirmDialog
            action={deleteReviewAction.bind(null, r.id)}
            title="Delete this review?"
            description="This also reverses its effect on the reviewee's average rating."
            confirmLabel="Delete"
            variant="danger"
            triggerLabel="Delete"
          />
        </div>
      ),
      className: "text-right",
    },
  ];

  return (
    <div>
      <PageHeader title="Reviews" description="Moderate reviews — deleting one reverses its effect on the reviewee's rating." />
      <Table columns={columns} rows={result.items} rowKey={(r) => r.id} emptyMessage="No reviews yet." />
      <Pagination page={result.page} totalPages={result.totalPages} totalCount={result.totalCount} searchParams={{ page: pageParam }} />
    </div>
  );
}
