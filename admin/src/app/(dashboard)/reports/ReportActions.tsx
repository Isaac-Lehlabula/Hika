"use client";

import { ConfirmDialog } from "@/components/ConfirmDialog";
import { resolveReportAction, dismissReportAction } from "./actions";

export function ReportActions({ reportId }: { reportId: string }) {
  return (
    <div className="flex justify-end gap-2">
      <ConfirmDialog
        action={() => dismissReportAction(reportId)}
        title="Dismiss this report?"
        description="No further action will be taken."
        confirmLabel="Dismiss"
        variant="secondary"
        triggerLabel="Dismiss"
      />
      <ConfirmDialog
        action={() => resolveReportAction(reportId)}
        title="Mark this report as resolved?"
        confirmLabel="Resolve"
        variant="primary"
        triggerLabel="Resolve"
      />
    </div>
  );
}
