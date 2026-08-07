"use server";

import { revalidatePath } from "next/cache";
import { refundPayment } from "@/lib/admin-api";

export async function refundPaymentAction(bookingId: string, formData: FormData) {
  const reason = String(formData.get("reason") ?? "").trim();
  if (!reason) {
    return;
  }
  await refundPayment(bookingId, reason);
  revalidatePath("/payments");
}
