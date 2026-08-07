import Link from "next/link";

export function Pagination({
  page,
  totalPages,
  totalCount,
  searchParams,
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  searchParams: Record<string, string | undefined>;
}) {
  if (totalPages <= 1) {
    return <p className="px-1 py-3 text-sm text-text-secondary">{totalCount} total</p>;
  }

  const hrefFor = (targetPage: number) => {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(searchParams)) {
      if (value) params.set(key, value);
    }
    params.set("page", String(targetPage));
    return `?${params.toString()}`;
  };

  return (
    <div className="flex items-center justify-between px-1 py-3 text-sm text-text-secondary">
      <span>
        Page {page} of {totalPages} · {totalCount} total
      </span>
      <div className="flex gap-2">
        <Link
          href={hrefFor(Math.max(1, page - 1))}
          aria-disabled={page <= 1}
          className={`rounded-lg border border-border px-3 py-1.5 ${page <= 1 ? "pointer-events-none opacity-40" : "hover:bg-surface-alt"}`}
        >
          Previous
        </Link>
        <Link
          href={hrefFor(Math.min(totalPages, page + 1))}
          aria-disabled={page >= totalPages}
          className={`rounded-lg border border-border px-3 py-1.5 ${page >= totalPages ? "pointer-events-none opacity-40" : "hover:bg-surface-alt"}`}
        >
          Next
        </Link>
      </div>
    </div>
  );
}
