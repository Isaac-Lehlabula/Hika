# Implementation Roadmap

Each phase: explain → implement → automated tests → `dotnet build`/`dotnet test` (and `npm run build`/lint for frontend work) → fix → update docs. Status reflects what's actually built, not what's planned.

| Phase | Scope | Status |
|---|---|---|
| 0 | Architecture & domain design docs (`/docs`) | ✅ Done |
| 1 | Solution/infra scaffolding — projects, Serilog, health checks, OpenAPI, EF plumbing, CORS | 🔶 In progress |
| 2 | Auth & users — registration, login, logout, email/phone verification, refresh tokens, password reset, profile; matching frontend screens | ⬜ Not started |
| 3 | Driver profiles & vehicles — license info, vehicle CRUD, vehicle photos, verification submission | ⬜ Not started |
| 4 | Trips, stops, segments — post a trip, intermediate stops, segment inventory creation | ⬜ Not started |
| 5 | Search — Find a Hike, filters/sorting, location autocomplete | ⬜ Not started |
| 6 | Bookings & seat inventory — request/accept/decline, concurrency-safe reservation, cancellation | ⬜ Not started |
| 7 | Payments abstraction — `IPaymentGateway`, mock provider, fare/fee/payout split, refunds | ⬜ Not started |
| 8 | Ratings & reviews — post-completion reviews, rating aggregation | ⬜ Not started |
| 9 | Notifications & ride alerts — dispatcher, email/SMS/push/in-app, alert matching | ⬜ Not started |
| 10 | Trust & safety — reports, blocks, emergency contacts, trip sharing | ⬜ Not started |
| 11 | Admin platform — user/trip/booking/payment/verification/report management, analytics, audit log viewer | ⬜ Not started |
| 12 | Production hardening — CI, rate limiting review, dependency scanning, load-relevant indexes, real payment/SMS/verification provider integration planning | ⬜ Not started |

## What's runnable today

This section is updated as each phase actually completes — see the status column above for ground truth.

## Next up

**Phase 1 — Solution/infra scaffolding**, currently in progress. Then **Phase 2 — Auth & users**, which builds directly on it. **Phase 3 — Driver profiles & vehicles** builds on Phase 2 (a `DriverProfile` is 1:1 with `ApplicationUser`) and is itself a prerequisite for Phase 4 (a `Trip` needs a driver + vehicle).
