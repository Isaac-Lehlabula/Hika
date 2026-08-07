"use client";

import { useActionState } from "react";
import { useFormStatus } from "react-dom";
import { updatePlatformFeeAction, type UpdateFeeState } from "./actions";

function SubmitButton() {
  const { pending } = useFormStatus();
  return (
    <button
      type="submit"
      disabled={pending}
      className="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white hover:bg-primary-dark disabled:opacity-60"
    >
      {pending ? "Saving…" : "Save"}
    </button>
  );
}

const initialState: UpdateFeeState = {};

export function PlatformFeeForm({ currentRatePercent }: { currentRatePercent: number }) {
  const [state, formAction] = useActionState(updatePlatformFeeAction, initialState);

  return (
    <form action={formAction} className="flex items-end gap-3">
      <div className="flex flex-col gap-1.5">
        <label htmlFor="percent" className="text-sm font-medium text-text-secondary">
          Platform fee (%)
        </label>
        <input
          id="percent"
          name="percent"
          type="number"
          min={0}
          max={100}
          step={0.1}
          defaultValue={currentRatePercent}
          className="w-32 rounded-lg border border-border bg-surface px-3 py-2 text-sm outline-none focus:border-primary"
        />
      </div>
      <SubmitButton />
      {state.error && <p className="text-sm text-danger">{state.error}</p>}
      {state.success && <p className="text-sm text-success">Saved.</p>}
    </form>
  );
}
