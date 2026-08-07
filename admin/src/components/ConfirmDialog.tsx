"use client";

import { useRef, useTransition } from "react";

const VARIANT_CLASSES: Record<"primary" | "danger" | "secondary", string> = {
  primary: "bg-primary text-white hover:bg-primary-dark",
  danger: "bg-danger text-white hover:opacity-90",
  secondary: "border border-border text-text-primary hover:bg-surface-alt",
};

export function ConfirmDialog({
  action,
  title,
  description,
  confirmLabel,
  variant = "primary",
  triggerLabel,
}: {
  action: () => Promise<void>;
  title: string;
  description?: string;
  confirmLabel: string;
  variant?: "primary" | "danger" | "secondary";
  triggerLabel: string;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [isPending, startTransition] = useTransition();

  return (
    <>
      <button
        onClick={() => dialogRef.current?.showModal()}
        className={`rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${VARIANT_CLASSES[variant]}`}
      >
        {triggerLabel}
      </button>
      <dialog
        ref={dialogRef}
        className="w-full max-w-sm rounded-2xl border border-border bg-surface p-6 text-text-primary shadow-xl backdrop:bg-black/40"
      >
        <h2 className="text-base font-semibold">{title}</h2>
        {description && <p className="mt-1 text-sm text-text-secondary">{description}</p>}
        <div className="mt-4 flex justify-end gap-2">
          <button
            onClick={() => dialogRef.current?.close()}
            className="rounded-lg border border-border px-3 py-1.5 text-sm hover:bg-surface-alt"
          >
            Cancel
          </button>
          <button
            disabled={isPending}
            onClick={() => startTransition(async () => {
              await action();
              dialogRef.current?.close();
            })}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-60 ${VARIANT_CLASSES[variant]}`}
          >
            {isPending ? "Working…" : confirmLabel}
          </button>
        </div>
      </dialog>
    </>
  );
}
