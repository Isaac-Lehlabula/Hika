"use server";

import { revalidatePath } from "next/cache";
import { resolveReport, dismissReport } from "@/lib/admin-api";

export async function resolveReportAction(reportId: string) {
  await resolveReport(reportId);
  revalidatePath("/reports");
}

export async function dismissReportAction(reportId: string) {
  await dismissReport(reportId);
  revalidatePath("/reports");
}
