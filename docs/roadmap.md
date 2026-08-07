# Implementation Roadmap

The primary product is the **Flutter mobile app**; the Next.js admin portal is secondary and deferred. From Phase 2 onward, each feature phase is built **vertically** — backend endpoints and the corresponding mobile screens land together, so the product stays usable (in the app) throughout development, not "backend-complete, then months of frontend work."

Each phase: explain → implement (backend + mobile together, from Phase 2 on) → automated tests (backend: xUnit; mobile: widget/unit tests as the app grows) → build/run → fix → update docs.

| Phase | Scope | Status |
|---|---|---|
| 0 | Architecture & domain design docs (`/docs`) | ✅ Done |
| 1 | Backend solution/infra scaffolding — projects, Serilog, health checks, OpenAPI, EF plumbing, CORS | ✅ Done |
| 2 | **Auth & users** — backend: registration, login, logout, email/phone verification, refresh tokens, password reset, profile. Mobile: Flutter scaffold (design system, networking, secure token storage), Register/Login/VerifyEmail/VerifyPhone/ForgotPassword/ResetPassword/Profile screens. | ✅ Done |
| 3 | **Driver profiles & vehicles** — backend: driver profile, vehicle CRUD, verification submission. Mobile: "Become a driver" flow, vehicle management screens. | 🔶 Backend done, mobile in progress |
| 4 | **Trips, stops, segments** — backend: post a trip, intermediate stops, segment inventory creation. Mobile: guided "post a trip" flow, trip detail screen. | ⬜ Not started |
| 5 | **Search** — backend: Find a Hike query, filters/sorting, location autocomplete. Mobile: home screen search, results list with trip cards. | ⬜ Not started |
| 6 | **Bookings & seat inventory** — backend: request/accept/decline, concurrency-safe reservation, cancellation. Mobile: reserve-seat flow, driver's incoming-requests screen. | ⬜ Not started |
| 7 | **Payments abstraction** — backend: `IPaymentGateway`, mock provider, fare/fee/payout split, refunds. Mobile: payment status/receipt UI. | ⬜ Not started |
| 8 | **Ratings & reviews** — backend: post-completion reviews, rating aggregation. Mobile: review submission, rating display. | ⬜ Not started |
| 9 | **Notifications & ride alerts** — backend: dispatcher, device-token registration, email/SMS/push/in-app, alert matching. Mobile: push notification wiring (FCM), in-app inbox, ride alert creation. | ⬜ Not started |
| 10 | **Trust & safety** — backend: reports, blocks, emergency contacts. Mobile: report/block UI, emergency contact management. | ⬜ Not started |
| 11 | **Admin portal** (Next.js, `admin/`) — user/trip/booking/payment/verification/report management, analytics, audit log viewer. Starts only once the mobile app's core flows (Phases 2–6) work end-to-end. | ⬜ Not started |
| 12 | **Production hardening** — CI (backend + mobile), rate limiting review, dependency scanning, load-relevant indexes, app store release prep (signing, store listings), real payment/SMS/verification provider integration. | ⬜ Not started |

## What's runnable today

- Backend: `docker compose up -d postgres mailhog`, then `dotnet run --project backend/src/Hika.Api` — full auth flow (register → verify email → verify phone OTP → login → refresh → profile edit → logout) works end-to-end against a real Postgres database. 55 automated tests passing (45 unit, 10 integration against a real Postgres via Testcontainers).
- Mobile: `flutter run` (see `mobile/hika_app/README.md`) — Register, Login, Verify Email, Verify Phone, Forgot/Reset Password, and Profile screens, all wired to the real backend and verified end-to-end (register → login → fetch profile against a live API). Home shows the flagship search UI (not yet wired to a backend); Trips/Bookings/Inbox show honest "coming soon" states; bottom-nav shell in place.

## Next up

**Phase 3 — Driver profiles & vehicles**, which builds on Phase 2 (a `DriverProfile` is 1:1 with `ApplicationUser`) and is itself a prerequisite for Phase 4 (a `Trip` needs a driver + vehicle) — backend endpoints and the "Become a driver" / vehicle-management Flutter screens land together.
