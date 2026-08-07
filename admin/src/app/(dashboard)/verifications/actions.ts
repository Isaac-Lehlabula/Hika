"use server";

import { revalidatePath } from "next/cache";
import { approveVerification, rejectVerification } from "@/lib/admin-api";

export async function approveVerificationAction(verificationId: string) {
  await approveVerification(verificationId);
  revalidatePath("/verifications");
}

export async function rejectVerificationAction(verificationId: string, formData: FormData) {
  const reason = String(formData.get("reason") ?? "").trim();
  if (!reason) {
    return;
  }
  await rejectVerification(verificationId, reason);
  revalidatePath("/verifications");
}
