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
| 6 | **Bookings & seat inventory** — backend: request/accept/decline, concurrency-safe reservation, cancellation. Mobile: reserve-seat flow, driver's incoming-requests screen. | ⬜ Not started |
| 7 | **Payments abstraction** — backend: `IPaymentGateway`, mock provider, fare/fee/payout split, refunds. Mobile: payment status/receipt UI. | ⬜ Not started |
| 8 | **Ratings & reviews** — backend: post-completion reviews, rating aggregation. Mobile: review submission, rating display. | ⬜ Not started |
| 9 | **Notifications & ride alerts** — backend: dispatcher, device-token registration, email/SMS/push/in-app, alert matching. Mobile: push notification wiring (FCM), in-app inbox, ride alert creation. | ⬜ Not started |
| 10 | **Trust & safety** — backend: reports, blocks, emergency contacts. Mobile: report/block UI, emergency contact management. | ⬜ Not started |
| 11 | **Admin portal** (Next.js, `admin/`) — user/trip/booking/payment/verification/report management, analytics, audit log viewer. Starts only once the mobile app's core flows (Phases 2–6) work end-to-end. | ⬜ Not started |
| 12 | **Production hardening** — CI (backend + mobile), rate limiting review, dependency scanning, load-relevant indexes, app store release prep (signing, store listings), real payment/SMS/verification provider integration. | ⬜ Not started |

## What's runnable today

- Backend: `docker compose up -d postgres mailhog`, then `dotnet run --project backend/src/Hika.Api` — auth (register → verify email → verify phone OTP → login → refresh → profile edit → logout), driver profile/vehicle/photo/verification-submission flows, trip posting (locations, ordered stops, per-adjacent-stop-pair segment inventory, get/list/cancel), and search (`GET /api/v1/search/trips` matching a rider's from/to against any sub-leg of a trip's stops, with passengers/date/price/verified-driver filters and sorting; `GET /api/v1/search/locations` autocomplete over the seeded Location table; `GET /api/v1/search/popular-routes` aggregated live from posted trips, never hardcoded) all work end-to-end against a real Postgres database. 116 automated tests passing (79 unit, 37 integration against a real Postgres via Testcontainers).
- Mobile: `flutter run` (see `mobile/hika_app/README.md`) — auth screens, "Become a driver" (license details, license photo submission), vehicle management (add/view/delete, photo upload with a primary-photo picker, registration-document submission), trip posting (a guided 4-step "post a trip" flow — vehicle, route, details, review — plus a trip-detail screen with cancel), and search (the home screen's From/To fields open a location-autocomplete picker, Date/Passengers pickers, "Find a Hike" navigates to a sorted/filterable trip-card results list, and "Popular this month" is wired to real aggregated trip data instead of a hardcoded list) are all wired to the real backend and verified end-to-end. The Trips tab shows the driver's posted trips. Bookings/Inbox show honest "coming soon" states; bottom-nav shell in place. 25 automated widget/unit tests passing.

## Next up

**Phase 6 — Bookings & seat inventory**: backend request/accept/decline flow with the concurrency-safe (Postgres advisory-lock) segment reservation described in `domain-model.md` §4, mobile reserve-seat flow from a search result plus the driver's incoming-requests screen.
