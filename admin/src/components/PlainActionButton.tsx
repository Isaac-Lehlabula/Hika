"use client";

import { useTransition } from "react";

const VARIANT_CLASSES: Record<"primary" | "danger" | "secondary", string> = {
  primary: "bg-primary text-white hover:bg-primary-dark",
  danger: "bg-danger text-white hover:opacity-90",
  secondary: "border border-border text-text-primary hover:bg-surface-alt",
};

/** Fires immediately on click, no confirmation step — for reversible, low-stakes actions
 * (unsuspend, approve a verification) where a dialog would just be friction. */
export function PlainActionButton({
  action,
  label,
  pendingLabel = "Working…",
  variant = "primary",
}: {
  action: () => Promise<void>;
  label: string;
  pendingLabel?: string;
  variant?: "primary" | "danger" | "secondary";
}) {
  const [isPending, startTransition] = useTransition();

  return (
    <button
      disabled={isPending}
      onClick={() => startTransition(action)}
      className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-60 ${VARIANT_CLASSES[variant]}`}
    >
      {isPending ? pendingLabel : label}
    </button>
  );
}
