# Implementation Roadmap

The primary product is the **Flutter mobile app**; the Next.js admin portal is secondary and deferred. From Phase 2 onward, each feature phase is built **vertically** — backend endpoints and the corresponding mobile screens land together, so the product stays usable (in the app) throughout development, not "backend-complete, then months of frontend work."

Each phase: explain → implement (backend + mobile together, from Phase 2 on) → automated tests (backend: xUnit; mobile: widget/unit tests as the app grows) → build/run → fix → update docs.

| Phase | Scope | Status |
|---|---|---|
| 0 | Architecture & domain design docs (`/docs`) | ✅ Done |
| 1 | Backend solution/infra scaffolding — projects, Serilog, health checks, OpenAPI, EF plumbing, CORS | ✅ Done |
| 2 | **Auth & users** — backend: registration, login, logout, email/phone verification, refresh tokens, password reset, profile. Mobile: Flutter scaffold (design system, networking, secure token storage), Register/Login/VerifyEmail/VerifyPhone/ForgotPassword/ResetPassword/Profile screens. | ✅ Done |
| 3 | **Driver profiles & vehicles** — backend: driver profile, vehicle CRUD, verification submission. Mobile: "Become a driver" flow, vehicle management screens. | ✅ Done |
| 4 | **Trips, stops, segments** — backend: post a trip, intermediate stops, segment inventory creation. Mobile: guided "post a trip" flow, trip detail screen. | ✅ Done |
| 5 | **Search** — backend: Find a Hike query, filters/sorting, location autocomplete. Mobile: home screen search, results list with trip cards. | ✅ Done |
| 6 | **Bookings & seat inventory** — backend: request/accept/decline, concurrency-safe reservation, cancellation. Mobile: reserve-seat flow, driver's incoming-requests screen. | ✅ Done |
| 7 | **Payments abstraction** — backend: `IPaymentGateway`, mock provider, fare/fee/payout split, refunds. Mobile: payment status/receipt UI. | ⬜ Not started |
| 8 | **Ratings & reviews** — backend: post-completion reviews, rating aggregation. Mobile: review submission, rating display. | ⬜ Not started |
| 9 | **Notifications & ride alerts** — backend: dispatcher, device-token registration, email/SMS/push/in-app, alert matching. Mobile: push notification wiring (FCM), in-app inbox, ride alert creation. | ⬜ Not started |
| 10 | **Trust & safety** — backend: reports, blocks, emergency contacts. Mobile: report/block UI, emergency contact management. | ⬜ Not started |
| 11 | **Admin portal** (Next.js, `admin/`) — user/trip/booking/payment/verification/report management, analytics, audit log viewer. Starts only once the mobile app's core flows (Phases 2–6) work end-to-end. | ⬜ Not started |
| 12 | **Production hardening** — CI (backend + mobile), rate limiting review, dependency scanning, load-relevant indexes, app store release prep (signing, store listings), real payment/SMS/verification provider integration. | ⬜ Not started |

## What's runnable today

- Backend: `docker compose up -d postgres mailhog`, then `dotnet run --project backend/src/Hika.Api` — auth (register → verify email → verify phone OTP → login → refresh → profile edit → logout), driver profile/vehicle/photo/verification-submission flows, trip posting (locations, ordered stops, per-adjacent-stop-pair segment inventory, get/list/cancel), search (`GET /api/v1/search/trips` matching a rider's from/to against any sub-leg of a trip's stops; location autocomplete; live popular-routes aggregation), and bookings (`POST /api/v1/bookings` reserves seats on a specific boarding→alighting range with a Postgres-advisory-lock-guarded transaction so two riders can never win the same last seat, plus accept/decline/cancel/complete and the driver's incoming-requests view) all work end-to-end against a real Postgres database. 141 automated tests passing (90 unit, 51 integration against a real Postgres via Testcontainers) — including a test that fires two concurrent booking requests for a trip's last seat and asserts exactly one succeeds.
- Mobile: `flutter run` (see `mobile/hika_app/README.md`) — auth screens, "Become a driver" (license details, license photo submission), vehicle management (add/view/delete, photo upload with a primary-photo picker, registration-document submission), trip posting (a guided 4-step "post a trip" flow — vehicle, route, details, review — plus a trip-detail screen with cancel), search (the home screen's From/To fields open a location-autocomplete picker, Date/Passengers pickers, "Find a Hike" navigates to a sorted/filterable trip-card results list, and "Popular this month" is wired to real aggregated trip data instead of a hardcoded list), and bookings (a reserve-seat screen on any non-owned scheduled trip — pick boarding/alighting stops and a seat count bounded by the real per-leg availability, submit a request — a Bookings tab listing the passenger's own requests/confirmations with a detail screen and cancel, and the driver's incoming-requests screen with accept/decline) are all wired to the real backend and verified end-to-end. Inbox shows an honest "coming soon" state; bottom-nav shell in place. 29 automated widget/unit tests passing.

## Next up

**Phase 7 — Payments abstraction**: backend `IPaymentGateway` port with a `MockPaymentGateway` MVP implementation, fare/platform-fee/driver-payout split, refunds; mobile payment status/receipt UI on a confirmed booking.
