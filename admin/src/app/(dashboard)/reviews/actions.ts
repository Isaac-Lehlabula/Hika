"use server";

import { revalidatePath } from "next/cache";
import { deleteReview } from "@/lib/admin-api";

export async function deleteReviewAction(reviewId: string) {
  await deleteReview(reviewId);
  revalidatePath("/reviews");
}
