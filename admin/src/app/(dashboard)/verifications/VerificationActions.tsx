"use client";

import { PlainActionButton } from "@/components/PlainActionButton";
import { ReasonDialog } from "@/components/ReasonDialog";
import { approveVerificationAction, rejectVerificationAction } from "./actions";

export function VerificationActions({ verificationId }: { verificationId: string }) {
  return (
    <div className="flex justify-end gap-2">
      <ReasonDialog
        action={rejectVerificationAction.bind(null, verificationId)}
        fieldName="reason"
        title="Reject this document"
        confirmLabel="Reject"
        variant="secondary"
        triggerLabel="Reject"
      />
      <PlainActionButton action={() => approveVerificationAction(verificationId)} label="Approve" variant="primary" />
    </div>
  );
}
