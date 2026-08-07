"use client";

import { ReasonDialog } from "@/components/ReasonDialog";
import { PlainActionButton } from "@/components/PlainActionButton";
import { suspendUserAction, unsuspendUserAction } from "./actions";

export function SuspendUserButton({ userId, isSuspended }: { userId: string; isSuspended: boolean }) {
  if (isSuspended) {
    return <PlainActionButton action={() => unsuspendUserAction(userId)} label="Unsuspend" variant="secondary" />;
  }

  return (
    <ReasonDialog
      action={suspendUserAction.bind(null, userId)}
      fieldName="reason"
      title="Suspend this user"
      description="They won't be able to book or drive until unsuspended."
      confirmLabel="Suspend"
      variant="danger"
      triggerLabel="Suspend"
    />
  );
}
