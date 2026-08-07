"use server";

import { revalidatePath } from "next/cache";
import { updatePlatformFee } from "@/lib/admin-api";

export type UpdateFeeState = { error?: string; success?: boolean };

export async function updatePlatformFeeAction(_prevState: UpdateFeeState, formData: FormData): Promise<UpdateFeeState> {
  const percent = Number(formData.get("percent"));
  if (!Number.isFinite(percent) || percent < 0 || percent > 100) {
    return { error: "Enter a percentage between 0 and 100." };
  }

  await updatePlatformFee(percent / 100);
  revalidatePath("/settings/platform-fee");
  return { success: true };
}
