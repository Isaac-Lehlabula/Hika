"use client";

import { useRef, useState, useTransition } from "react";

const VARIANT_CLASSES: Record<"primary" | "danger" | "secondary", string> = {
  primary: "bg-primary text-white hover:bg-primary-dark",
  danger: "bg-danger text-white hover:opacity-90",
  secondary: "border border-border text-text-primary hover:bg-surface-alt",
};

/** A row-scoped action that needs one piece of free text first (a suspension reason, a
 * rejection reason, a refund reason). Uses the native <dialog> element rather than
 * window.prompt — a table row has no room for an inline text field, and an actual dialog is
 * both better UX (styleable, not a jarring browser-chrome popup) and automatable in ways a
 * blocking prompt() call is not. */
export function ReasonDialog({
  action,
  fieldName,
  title,
  description,
  confirmLabel,
  variant = "primary",
  triggerLabel,
}: {
  action: (formData: FormData) => Promise<void>;
  fieldName: string;
  title: string;
  description?: string;
  confirmLabel: string;
  variant?: "primary" | "danger" | "secondary";
  triggerLabel: string;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [reason, setReason] = useState("");
  const [isPending, startTransition] = useTransition();

  function close() {
    setReason("");
    dialogRef.current?.close();
  }

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
        onClose={() => setReason("")}
      >
        <h2 className="text-base font-semibold">{title}</h2>
        {description && <p className="mt-1 text-sm text-text-secondary">{description}</p>}
        <textarea
          autoFocus
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          rows={3}
          placeholder="Reason…"
          className="mt-4 w-full rounded-lg border border-border bg-surface px-3 py-2 text-sm outline-none focus:border-primary"
        />
        <div className="mt-4 flex justify-end gap-2">
          <button onClick={close} className="rounded-lg border border-border px-3 py-1.5 text-sm hover:bg-surface-alt">
            Cancel
          </button>
          <button
            disabled={!reason.trim() || isPending}
            onClick={() => {
              const formData = new FormData();
              formData.set(fieldName, reason.trim());
              startTransition(async () => {
                await action(formData);
                close();
              });
            }}
            className={`rounded-lg px-3 py-1.5 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-60 ${VARIANT_CLASSES[variant]}`}
          >
            {isPending ? "Working…" : confirmLabel}
          </button>
        </div>
      </dialog>
    </>
  );
}
