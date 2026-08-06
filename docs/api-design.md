# API Design

REST over HTTPS, JSON, versioned via URL prefix (`/api/v1/...`) from day one so breaking changes later don't disrupt the first mobile client. Full machine-readable contract is generated OpenAPI (served at `/openapi/v1.json`, browsable at `/scalar`) — this document is the human catalog of what exists and why, not a spec to keep byte-for-byte in sync.

## Conventions

- **Auth**: `Authorization: Bearer <jwt>` for authenticated endpoints. Public endpoints (search, trip details, register/login) need none.
- **Pagination**: list endpoints accept `page` (1-based) and `pageSize` (default 20, max 100), return `{ items, page, pageSize, totalCount }`.
- **Errors**: RFC 7807 `ProblemDetails` for every non-2xx response, with a `traceId` for correlating with server logs. Validation failures use the `errors` extension (field → messages).
- **Idempotency**: mutating endpoints that could plausibly be retried by a flaky mobile connection (booking creation, payment initiation) accept an optional `Idempotency-Key` header.
- **Concurrency**: entities with optimistic concurrency return an `ETag`; updates may send `If-Match`.

## Auth (`/api/v1/auth`)

| Method & path | Purpose |
|---|---|
| `POST /register` | Create account (email, password, first/last name, phone). Sends verification email. |
| `POST /login` | Email+password → access token + refresh token (refresh set as httpOnly cookie by the frontend BFF; API itself just returns it in the body for any client). |
| `POST /refresh` | Rotate a refresh token for a new access/refresh pair. |
| `POST /logout` | Revoke the presented refresh token (and optionally all sessions). |
| `POST /verify-email` | Consume an email verification token. |
| `POST /resend-verification-email` | Re-send if the first expired. |
| `POST /request-phone-otp` | Send an SMS OTP to the account's phone number. |
| `POST /verify-phone` | Consume the OTP. |
| `POST /forgot-password` | Issue a password reset token (always 202, never reveals whether the email exists). |
| `POST /reset-password` | Consume the reset token, set a new password. |

## Users (`/api/v1/users`)

| Method & path | Purpose |
|---|---|
| `GET /me` | Own full profile (private fields included). |
| `PUT /me` | Update own profile (name, photo, phone triggers re-verification). |
| `POST /me/photo` | Upload profile photo. |
| `GET /{userId}` | Public profile (name, photo, rating, completed trips, member-since, verification badges — no phone/email). |
| `GET /me/emergency-contacts`, `POST/PUT/DELETE /me/emergency-contacts/{id}` | Emergency contacts (Trust & Safety). |

## Drivers & vehicles (`/api/v1/drivers`)

| Method & path | Purpose |
|---|---|
| `POST /me/driver-profile` | Create/update driver profile (license info). |
| `GET /me/driver-profile` | Own driver profile + verification status. |
| `POST /me/vehicles`, `GET/PUT/DELETE /me/vehicles/{id}` | Manage vehicles. |
| `POST /me/vehicles/{id}/photos` | Upload vehicle photos. |
| `POST /me/driver-profile/verification-documents` | Submit license/ID documents for review. |
| `POST /me/vehicles/{id}/verification-documents` | Submit vehicle registration documents. |

## Trips (`/api/v1/trips`)

| Method & path | Purpose |
|---|---|
| `POST /` | Driver creates a trip (origin, destination, stops, date/time, seats, price, vehicle, luggage, notes). |
| `GET /{tripId}` | Full trip detail (driver profile, vehicle, stops, per-segment availability). |
| `PUT /{tripId}` | Edit an upcoming trip (only while `Scheduled` and no confirmed bookings conflict). |
| `POST /{tripId}/cancel` | Cancel a trip (cascades: declines pending bookings, notifies confirmed passengers, triggers refunds). |
| `GET /me` | Trips I'm driving. |

## Search (`/api/v1/search`)

| Method & path | Purpose |
|---|---|
| `GET /trips?from=&to=&date=&passengers=&sort=&verifiedOnly=&maxPrice=` | Core "Find a Hike" query — matches trips with a contiguous stop-range covering `from`→`to` with enough segment-level availability. `sort` ∈ `departureTime, price, driverRating, seatsAvailable, pickupDistance`. |
| `GET /locations?query=` | Autocomplete against the `Location` table (falls back gracefully — the client can still submit free text). |
| `GET /popular-routes?month=` | Aggregated from real `Trip`/`Booking` activity (see §"Popular Routes" below) — never hardcoded. |

## Bookings (`/api/v1/bookings`)

| Method & path | Purpose |
|---|---|
| `POST /` | Passenger requests seats on a trip (stop range + seat count). Runs the concurrency-safe reservation; returns `Pending`. |
| `GET /me` | My bookings (as passenger). |
| `GET /{bookingId}` | Booking detail. |
| `POST /{bookingId}/cancel` | Passenger cancels (before or after confirmation — different downstream effects). |
| `GET /trips/{tripId}/requests` | Driver's incoming booking requests for a trip. |
| `POST /{bookingId}/accept` | Driver accepts → `Confirmed`, triggers payment capture + notification. |
| `POST /{bookingId}/decline` | Driver declines → `Declined`, releases held seats, notification. |
| `POST /{bookingId}/complete` | Marks a booking `Completed` after the trip date (system/driver-triggered), unlocking reviews. |

## Payments (`/api/v1/payments`)

| Method & path | Purpose |
|---|---|
| `GET /bookings/{bookingId}/payment` | Payment status/receipt for a booking. |
| `POST /bookings/{bookingId}/refund` | Admin/driver-initiated refund (policy-gated). |
| `GET /me/payouts` | Driver's payout history. |

## Reviews (`/api/v1/reviews`)

| Method & path | Purpose |
|---|---|
| `POST /bookings/{bookingId}` | Submit a review (only valid once `Booking.Status == Completed`; direction inferred from caller's role in the booking). |
| `GET /users/{userId}` | Paginated reviews received by a user. |

## Notifications & ride alerts (`/api/v1/notifications`, `/api/v1/ride-alerts`)

| Method & path | Purpose |
|---|---|
| `GET /notifications/me` | In-app notification feed. |
| `POST /notifications/me/{id}/read` | Mark read. |
| `POST /ride-alerts` | Create an alert ("notify me when someone posts JHB → Giyani for 20 Dec"). |
| `GET /ride-alerts/me`, `DELETE /ride-alerts/{id}` | Manage own alerts. |

## Trust & safety (`/api/v1/trust-safety`)

| Method & path | Purpose |
|---|---|
| `POST /reports` | Report a user or trip. |
| `POST /blocks/{userId}`, `DELETE /blocks/{userId}` | Block/unblock. |

## Admin (`/api/v1/admin/...`, requires `Admin` policy)

| Method & path | Purpose |
|---|---|
| `GET /users`, `POST /users/{id}/suspend` | User management. |
| `GET /verifications`, `POST /verifications/{id}/approve`, `POST /verifications/{id}/reject` | Verification review queue. |
| `GET /trips`, `POST /trips/{id}/remove` | Trip oversight. |
| `GET /bookings`, `GET /payments`, `POST /payments/{id}/refund` | Financial oversight. |
| `GET /reports`, `POST /reports/{id}/resolve` | Safety report handling. |
| `GET /reviews`, `DELETE /reviews/{id}` | Review moderation. |
| `GET /platform-fees`, `PUT /platform-fees` | Fee configuration. |
| `GET /analytics/overview` | Basic platform analytics (trip volume, GMV, active users — derived from existing tables, not a separate analytics store at MVP scale). |
| `GET /audit-logs` | Read the audit trail. |

**Popular routes**, mentioned in the product brief's home-screen concept, are computed by aggregating `Trip`/`Booking` rows over a recent rolling window (e.g. `GROUP BY origin_location, destination_location ORDER BY count DESC` over the last N days, refreshed on read or via a cheap periodic materialized view once volume justifies it) — never a hardcoded list, per the explicit requirement.

## Authorization policies

- `AuthenticatedUser` — any logged-in user.
- `EmailVerified` — required for posting a trip or making a booking (not for browsing/search).
- `VerifiedDriver` — required to post a trip (driver profile + at least one verified vehicle).
- `Admin` — staff-only endpoints, itself split into finer policies later if the admin team grows past "trusted small group" (e.g. read-only support staff vs. staff who can issue refunds).

## Mobile-readiness

Nothing above is web-specific: no server-rendered HTML, no session-cookie-only auth (the JWT/refresh model works for a native client directly; only the *web* app additionally wraps it in httpOnly cookies via its own BFF layer — see `frontend-architecture.md`). A future React Native or native iOS/Android client is simply another OpenAPI-generated consumer of this same surface.
