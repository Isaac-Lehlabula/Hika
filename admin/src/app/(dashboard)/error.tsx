"use client";

export default function DashboardError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <div className="rounded-xl border border-danger-light bg-danger-light px-6 py-8 text-center">
      <h2 className="text-base font-semibold text-danger">Something went wrong</h2>
      <p className="mt-1 text-sm text-danger/80">{error.message || "The backend request failed."}</p>
      <button
        onClick={reset}
        className="mt-4 rounded-lg border border-danger px-4 py-1.5 text-sm font-medium text-danger hover:bg-danger hover:text-white"
      >
        Try again
      </button>
    </div>
  );
}
