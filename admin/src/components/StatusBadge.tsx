const TONES: Record<string, string> = {
  positive: "bg-success-light text-success",
  negative: "bg-danger-light text-danger",
  warning: "bg-warning-light text-warning",
  neutral: "bg-surface-alt text-text-secondary",
};

const STATUS_TONE: Record<string, keyof typeof TONES> = {
  Scheduled: "neutral",
  InProgress: "warning",
  Completed: "positive",
  Cancelled: "negative",
  Pending: "warning",
  Confirmed: "positive",
  Declined: "negative",
  Succeeded: "positive",
  Failed: "negative",
  Refunded: "neutral",
  Verified: "positive",
  Rejected: "negative",
  NotSubmitted: "neutral",
  Open: "warning",
  UnderReview: "warning",
  Resolved: "positive",
  Dismissed: "neutral",
};

export function StatusBadge({ status }: { status: string }) {
  const tone = TONES[STATUS_TONE[status] ?? "neutral"];
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${tone}`}>{status}</span>
  );
}
