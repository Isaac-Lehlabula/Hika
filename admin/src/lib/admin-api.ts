import { adminFetch, buildQuery, PagedResult } from "./api";

// ---- Users ----

export type AdminUser = {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  emailVerified: boolean;
  phoneVerified: boolean;
  isAdmin: boolean;
  isSuspended: boolean;
  suspensionReason: string | null;
  averageRating: number | null;
  completedTripCount: number;
  memberSinceUtc: string;
};

export function getUsers(params: { search?: string; page?: number; pageSize?: number }) {
  return adminFetch<PagedResult<AdminUser>>(`/api/v1/admin/users${buildQuery(params)}`);
}

export function suspendUser(userId: string, reason: string) {
  return adminFetch<AdminUser>(`/api/v1/admin/users/${userId}/suspend`, {
    method: "POST",
    body: JSON.stringify({ reason }),
  });
}

export function unsuspendUser(userId: string) {
  return adminFetch<AdminUser>(`/api/v1/admin/users/${userId}/unsuspend`, { method: "POST" });
}

// ---- Verifications ----

export type AdminVerification = {
  id: string;
  subjectType: "User" | "Vehicle";
  subjectId: string;
  subjectDisplayName: string | null;
  type: "IdentityDocument" | "DriverLicense" | "VehicleRegistration";
  status: "NotSubmitted" | "Pending" | "Verified" | "Rejected";
  documentUrl: string | null;
  submittedAtUtc: string | null;
  reviewedAtUtc: string | null;
  rejectionReason: string | null;
};

export function getVerifications(params: { status?: string; page?: number; pageSize?: number }) {
  return adminFetch<PagedResult<AdminVerification>>(`/api/v1/admin/verifications${buildQuery(params)}`);
}

export function approveVerification(verificationId: string) {
  return adminFetch<AdminVerification>(`/api/v1/admin/verifications/${verificationId}/approve`, { method: "POST" });
}

export function rejectVerification(verificationId: string, reason: string) {
  return adminFetch<AdminVerification>(`/api/v1/admin/verifications/${verificationId}/reject`, {
    method: "POST",
    body: JSON.stringify({ reason }),
  });
}

// ---- Trips ----

export type AdminTrip = {
  id: string;
  driverName: string;
  driverUserId: string;
  originName: string;
  destinationName: string;
  departureAtUtc: string;
  status: "Scheduled" | "InProgress" | "Completed" | "Cancelled";
  totalSeatsOffered: number;
  pricePerSeat: number;
};

export function getTrips(params: { status?: string; page?: number; pageSize?: number }) {
  return adminFetch<PagedResult<AdminTrip>>(`/api/v1/admin/trips${buildQuery(params)}`);
}

export function removeTrip(tripId: string, reason: string) {
  return adminFetch<AdminTrip>(`/api/v1/admin/trips/${tripId}/remove`, {
    method: "POST",
    body: JSON.stringify({ reason }),
  });
}

// ---- Bookings ----

export type AdminBooking = {
  id: string;
  tripId: string;
  passengerName: string;
  driverName: string;
  status: "Pending" | "Confirmed" | "Declined" | "Cancelled" | "Completed";
  seatsRequested: number;
  totalPrice: number;
  requestedAtUtc: string;
};

export function getBookings(params: { status?: string; page?: number; pageSize?: number }) {
  return adminFetch<PagedResult<AdminBooking>>(`/api/v1/admin/bookings${buildQuery(params)}`);
}

// ---- Payments ----

export type AdminPayment = {
  id: string;
  bookingId: string;
  amount: number;
  platformFee: number;
  driverPayoutAmount: number;
  provider: string;
  providerReference: string | null;
  status: "Pending" | "Succeeded" | "Failed" | "Refunded";
  createdAtUtc: string;
};

export function getPayments(params: { status?: string; page?: number; pageSize?: number }) {
  return adminFetch<PagedResult<AdminPayment>>(`/api/v1/admin/payments${buildQuery(params)}`);
}

export function refundPayment(bookingId: string, reason: string) {
  return adminFetch<AdminPayment>(`/api/v1/admin/payments/bookings/${bookingId}/refund`, {
    method: "POST",
    body: JSON.stringify({ reason }),
  });
}

// ---- Reports ----

export type AdminReport = {
  id: string;
  reporterName: string;
  reporterUserId: string;
  reportedUserName: string | null;
  reportedUserId: string | null;
  reportedTripId: string | null;
  reason: string;
  description: string;
  status: "Open" | "UnderReview" | "Resolved" | "Dismissed";
  createdAtUtc: string;
};

export function getReports(params: { status?: string; page?: number; pageSize?: number }) {
  return adminFetch<PagedResult<AdminReport>>(`/api/v1/admin/reports${buildQuery(params)}`);
}

export function resolveReport(reportId: string) {
  return adminFetch<AdminReport>(`/api/v1/admin/reports/${reportId}/resolve`, { method: "POST" });
}

export function dismissReport(reportId: string) {
  return adminFetch<AdminReport>(`/api/v1/admin/reports/${reportId}/dismiss`, { method: "POST" });
}

// ---- Reviews ----

export type AdminReview = {
  id: string;
  bookingId: string;
  reviewerName: string;
  revieweeName: string;
  direction: "PassengerToDriver" | "DriverToPassenger";
  rating: number;
  comment: string | null;
  createdAtUtc: string;
};

export function getReviews(params: { page?: number; pageSize?: number }) {
  return adminFetch<PagedResult<AdminReview>>(`/api/v1/admin/reviews${buildQuery(params)}`);
}

export function deleteReview(reviewId: string) {
  return adminFetch<void>(`/api/v1/admin/reviews/${reviewId}`, { method: "DELETE" });
}

// ---- Platform fee ----

export type PlatformFee = {
  rate: number;
  updatedAtUtc: string;
  updatedByAdminUserId: string | null;
};

export function getPlatformFee() {
  return adminFetch<PlatformFee>("/api/v1/admin/platform-fees");
}

export function updatePlatformFee(rate: number) {
  return adminFetch<PlatformFee>("/api/v1/admin/platform-fees", {
    method: "PUT",
    body: JSON.stringify({ rate }),
  });
}

// ---- Analytics ----

export type AnalyticsOverview = {
  totalUsers: number;
  totalDrivers: number;
  totalTrips: number;
  scheduledTrips: number;
  totalBookings: number;
  completedBookings: number;
  grossMerchandiseValue: number;
  totalPlatformFees: number;
  activeUsersLast30Days: number;
  tripsPostedLast30Days: number;
  openReports: number;
  pendingVerifications: number;
};

export function getAnalyticsOverview() {
  return adminFetch<AnalyticsOverview>("/api/v1/admin/analytics/overview");
}

// ---- Audit log ----

export type AuditLogEntry = {
  id: string;
  adminUserId: string;
  adminName: string;
  action: string;
  entityType: string;
  entityId: string | null;
  details: string | null;
  createdAtUtc: string;
};

export function getAuditLogs(params: { page?: number; pageSize?: number }) {
  return adminFetch<PagedResult<AuditLogEntry>>(`/api/v1/admin/audit-logs${buildQuery(params)}`);
}
