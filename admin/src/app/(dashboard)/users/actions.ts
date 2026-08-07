"use server";

import { revalidatePath } from "next/cache";
import { suspendUser, unsuspendUser } from "@/lib/admin-api";

export async function suspendUserAction(userId: string, formData: FormData) {
  const reason = String(formData.get("reason") ?? "").trim();
  if (!reason) {
    return;
  }
  await suspendUser(userId, reason);
  revalidatePath("/users");
}

export async function unsuspendUserAction(userId: string) {
  await unsuspendUser(userId);
  revalidatePath("/users");
}
