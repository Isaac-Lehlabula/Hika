import type { ReactNode } from "react";

export type Column<T> = {
  header: string;
  cell: (row: T) => ReactNode;
  className?: string;
};

export function Table<T>({ columns, rows, rowKey, emptyMessage }: {
  columns: Column<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  emptyMessage: string;
}) {
  if (rows.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-border bg-surface px-6 py-12 text-center text-sm text-text-secondary">
        {emptyMessage}
      </div>
    );
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-border bg-surface">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-border bg-surface-alt text-xs font-semibold uppercase tracking-wide text-text-secondary">
            {columns.map((column) => (
              <th key={column.header} className="whitespace-nowrap px-4 py-3">
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={rowKey(row)} className="border-b border-border last:border-0 hover:bg-surface-alt/60">
              {columns.map((column) => (
                <td key={column.header} className={`px-4 py-3 align-middle ${column.className ?? ""}`}>
                  {column.cell(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
