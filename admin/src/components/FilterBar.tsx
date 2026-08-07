"use client";

import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { useState, useTransition } from "react";

export function FilterBar({
  searchPlaceholder,
  statusOptions,
}: {
  searchPlaceholder?: string;
  statusOptions?: { value: string; label: string }[];
}) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [search, setSearch] = useState(searchParams.get("search") ?? "");
  const [, startTransition] = useTransition();

  function updateParams(next: Record<string, string | undefined>) {
    const params = new URLSearchParams(searchParams.toString());
    for (const [key, value] of Object.entries(next)) {
      if (value) params.set(key, value);
      else params.delete(key);
    }
    params.delete("page");
    startTransition(() => router.push(`${pathname}?${params.toString()}`));
  }

  return (
    <div className="mb-4 flex flex-wrap items-center gap-3">
      {searchPlaceholder && (
        <form
          onSubmit={(event) => {
            event.preventDefault();
            updateParams({ search });
          }}
          className="flex items-center gap-2"
        >
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder={searchPlaceholder}
            className="w-64 rounded-lg border border-border bg-surface px-3 py-1.5 text-sm outline-none focus:border-primary"
          />
          <button type="submit" className="rounded-lg border border-border px-3 py-1.5 text-sm hover:bg-surface-alt">
            Search
          </button>
        </form>
      )}
      {statusOptions && (
        <select
          defaultValue={searchParams.get("status") ?? ""}
          onChange={(event) => updateParams({ status: event.target.value || undefined })}
          className="rounded-lg border border-border bg-surface px-3 py-1.5 text-sm outline-none focus:border-primary"
        >
          <option value="">All statuses</option>
          {statusOptions.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      )}
    </div>
  );
}
