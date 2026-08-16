import { getPlatformFee } from "@/lib/admin-api";
import { formatDateTime } from "@/lib/format";
import { PageHeader } from "@/components/PageHeader";
import { PlatformFeeForm } from "./PlatformFeeForm";

export default async function PlatformFeePage() {
  const fee = await getPlatformFee();

  return (
    <div>
      <PageHeader title="Platform fee" description="The share of every fare Hiking Spot keeps — applied to payments captured after a change." />
      <div className="max-w-md rounded-xl border border-border bg-surface p-6">
        <PlatformFeeForm currentRatePercent={fee.rate * 100} />
        <p className="mt-4 text-xs text-text-secondary">Last updated {formatDateTime(fee.updatedAtUtc)}.</p>
      </div>
    </div>
  );
}
