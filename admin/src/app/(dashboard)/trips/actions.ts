"use server";

import { revalidatePath } from "next/cache";
import { removeTrip } from "@/lib/admin-api";

export async function removeTripAction(tripId: string, formData: FormData) {
  const reason = String(formData.get("reason") ?? "").trim();
  if (!reason) {
    return;
  }
  await removeTrip(tripId, reason);
  revalidatePath("/trips");
}
