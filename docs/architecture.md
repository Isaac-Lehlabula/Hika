# Architecture

## 1. Product framing

Hiking Spot is **not** an on-demand ride-hailing app. A driver who is already making a trip posts it with available seats; passengers discover trips going their way and reserve seats — potentially for only part of the driver's route. This distinction shapes the domain model more than anything else: the core hard problem is not "match a driver to a passenger" (Uber's problem) but "track seat inventory across the sub-segments of a single journey" (closer to airline/bus segment inventory).

The **primary customer-facing product is a Flutter mobile app** (Android + iOS) — most South African users will reach Hiking Spot from a phone, and the product should feel like a native consumer app, not a mobile-rendered website. A Next.js **admin/operations portal** (internal staff only — verification review, dispute handling, analytics) is a separate, secondary application built later, once the mobile app's core flows work end-to-end. See [`mobile-architecture.md`](mobile-architecture.md) for the Flutter app's design.

## 2. High-level system

```
┌────────────────────┐
│ Flutter mobile app  │──┐
│ (mobile/)           │  │  REST + OpenAPI      ┌───────────────────────┐      ┌─────────────────┐
│ Android + iOS       │  ├──────────────────────▶│  ASP.NET Core Web API │─────▶│  PostgreSQL 16   │
└────────────────────┘  │  ◀──────────────────────│  (backend/)           │      └─────────────────┘
                         │                        └───────────────────────┘
┌────────────────────┐  │
│ Next.js admin portal│──┘
│ (admin/, later)     │
└────────────────────┘

Both clients speak the same REST+OpenAPI surface — the API has no notion of "web" vs "mobile",
so adding the admin portal later, or a second mobile platform, requires no backend redesign.
```

The backend is a **modular monolith**: one deployable ASP.NET Core process, internally organized into modules (Users, Drivers, Trips, Bookings, Payments, Reviews, Notifications, Trust & Safety, Admin) with disciplined boundaries between them. This is deliberately *not* microservices — at MVP scale, network-hop overhead, distributed transactions, and operational complexity would cost far more than they'd return. The module boundaries are drawn so that any module could be extracted into its own service later if it ever needs independent scaling or deployment, without a full redesign.

## 3. Why a modular monolith, not microservices or a "big ball of mud"

A single unstructured ASP.NET project (one big `Controllers` folder, one big `Services` folder) becomes unmaintainable once you have 9+ business areas — exactly the "not legacy MVC" problem the product owner wants to avoid. Full microservices, on the other hand, would force distributed transactions for something like "confirm a booking, decrement seat inventory, create a payment, send a notification" — a single-process transaction today, a saga tomorrow, for no current benefit. A modular monolith gets the maintainability benefit (clear seams, one module can't silently reach into another's tables) while keeping deployment, transactions, and local development simple.

## 4. Solution layout

```
backend/
  Hika.sln
  src/
    Hika.Domain/            Entities, value objects, enums, domain events. No dependencies on anything else.
    Hika.Application/       Use-case services, DTOs, FluentValidation validators, port interfaces
                             (IEmailSender, ISmsSender, IPaymentGateway, ILocationProvider, IFileStorage...).
                             Depends only on Hika.Domain.
    Hika.Infrastructure/     EF Core DbContext + entity configurations + migrations, repository
                             implementations (only where they add value — see §6), ASP.NET Core Identity
                             wiring, JWT issuance, adapters that implement the Application-layer ports
                             (SMTP email sender, Mailhog-friendly, console/log SMS sender, mock payment
                             gateway, local file storage). Depends on Domain + Application.
    Hika.Api/                Controllers, DI composition root, middleware pipeline, OpenAPI/Scalar,
                             appsettings. Depends on all of the above. Nothing depends on Hika.Api.
  tests/
    Hika.UnitTests/          Fast, no I/O — domain logic and application services with fakes.
    Hika.IntegrationTests/   Testcontainers-backed Postgres, real DbContext, WebApplicationFactory
                             hitting real controllers end-to-end.
```

Each of `Hika.Domain` and `Hika.Application` has a folder per module (`Users/`, `Drivers/`, `Trips/`, `Bookings/`, `Payments/`, `Reviews/`, `Notifications/`, `TrustSafety/`, `Admin/`, plus `Common/` for shared primitives like `Money`, `Province`, base entity types). `Hika.Api` mirrors this with one controller group per module. This is "module-per-folder", not "module-per-project" — see the trade-off below.

### Why folders, not a project per module

A project-per-module structure (`Hika.Modules.Trips.Domain`, `Hika.Modules.Trips.Application`, ...) is the textbook way to make module boundaries compiler-enforced, but for an MVP with 9 modules that's 25-35 csproj files before a single feature exists — real overengineering for the current team size (one developer). Folder-per-module inside four projects gets most of the same clarity (you can see instantly which module a class belongs to) with a fraction of the ceremony. The discipline that *is* enforced: a module's Domain/Application code must not directly reference another module's entities — it goes through the other module's Application-layer interface, or through IDs only (e.g., `Booking.PassengerUserId` is a `Guid`, not a foreign navigation to `ApplicationUser`, unless EF genuinely needs the join). When/if a module needs to scale or deploy independently, the folder becomes a project with minimal churn because the boundary discipline already exists.

## 5. API style: Controllers, not Minimal APIs

"Legacy ASP.NET MVC" — which the product brief explicitly wants to avoid — refers to server-rendered, `View`/Razor-based MVC (`return View()`, ViewBags, server-side HTML generation). That is not what's used here. `Hika.Api` uses **attribute-routed Web API controllers** (`ControllerBase`, `[ApiController]`, JSON in/out, no views) — a fully modern, current pattern, distinct from legacy MVC. Minimal APIs were considered: they're more modern-looking in tiny samples, but at 9+ resource areas with many endpoints each, they need an extra grouping/organization convention (e.g. Carter or FastEndpoints) to avoid a 1000-line `Program.cs` or an ad-hoc set of `Endpoints.cs` files — at which point you've re-invented controllers with extra steps. `[ApiController]` also gives automatic model-validation-to-`ProblemDetails` conversion and consistent conventions for free, which matters given the "ProblemDetails responses" and "proper validation" requirements.

## 6. Repositories: only where they earn their keep

EF Core's `DbContext`/`DbSet<T>` already *is* a repository + unit-of-work pair. Wrapping every entity in an `IFooRepository` that just forwards to `DbSet<Foo>` adds a layer that provides no real abstraction (you can't meaningfully swap EF Core for something else in this project) and actively hides useful EF features (`Include`, projections, `AsNoTracking`). So: **no generic repository layer**. Where a repository-shaped abstraction *does* earn its keep — the seat-inventory logic in Trips/Bookings, which needs a specific, carefully-controlled query+lock pattern (see `docs/domain-model.md` §4) — it's wrapped in a small, purpose-built class (e.g. `ISeatInventoryService`) that expresses the actual operation ("try to reserve N seats for this segment range"), not a generic `IRepository<T>`.

## 7. Cross-module decoupling: a small domain-event dispatcher, not MediatR

MediatR and AutoMapper (both maintained by the same author/organization) moved to a commercial license tier for paid/larger organizations starting in 2025. To avoid licensing risk in what's meant to become a real product, and because the actual need is small, this project uses:
- **Manual mapping**: small `ToDto()`/`ToResponse()` extension methods next to the DTOs they produce. No reflection-based magic, easy to debug, trivially testable.
- **A minimal in-process domain event dispatcher** (~30 lines, in `Hika.Application/Common`) for the few cases where one module needs to react to another without a hard reference — e.g. `BookingConfirmed` should trigger a notification and (later) unlock review eligibility, without `Hika.Application.Bookings` taking a dependency on `Hika.Application.Notifications`. Handlers are registered in DI and invoked synchronously within the same transaction/request for now (no message bus — unnecessary at this scale); the interface is intentionally small enough (`IDomainEventHandler<T>`) that swapping in an outbox/message bus later is a contained change.

## 8. Cross-cutting concerns

- **Logging**: Serilog, structured (JSON in production, readable console in dev), enriched with request/correlation IDs. Chosen over built-in `ILogger` alone because sinks (file, and later a log aggregator) and structured enrichment are configured declaratively and are a de facto standard in production .NET.
- **Validation**: FluentValidation validators per request DTO, invoked via an action filter/pipeline so controllers stay thin; failures surface as RFC 7807 `ProblemDetails` with field-level errors.
- **Error handling**: a single global exception-handling middleware (`IExceptionHandler`, .NET's built-in hook) maps domain/application exceptions to appropriate HTTP status + `ProblemDetails`, and anything unexpected to a generic 500 without leaking internals — logged with full detail server-side.
- **Health checks**: `/health/live` (process is up) and `/health/ready` (dependencies, i.e. the database, are reachable) via `Microsoft.Extensions.Diagnostics.HealthChecks` — suitable for container orchestrators later.
- **API docs**: `Microsoft.AspNetCore.OpenApi` (built into ASP.NET Core since .NET 9) generates the OpenAPI document; **Scalar** renders an interactive reference UI at `/scalar`. This is the current idiomatic .NET approach and is what a mobile client or any future consumer would use to generate a typed client.
- **Pagination**: a shared `PagedRequest`/`PagedResult<T>` convention (page/pageSize query params, total count in the response envelope) used by every list endpoint (trip search, admin lists, notifications, reviews).
- **CancellationToken**: threaded through every async controller action and application/infrastructure method, per ASP.NET Core convention, so client disconnects/timeouts cancel in-flight DB work.
- **AuditLog**: sensitive/admin-affecting operations write an `AuditLog` row via a small `IAuditLogger` service (actor, action, entity, before/after where relevant, timestamp, IP).

## 9. Configuration & secrets

Configuration follows standard ASP.NET Core layering: `appsettings.json` (safe defaults/non-secrets) → `appsettings.{Environment}.json` → environment variables (used in Docker/Compose and in any future cloud deployment) → user-secrets for local dev (`dotnet user-secrets`, never committed). Nothing secret (JWT signing key, DB password, future payment gateway keys) is ever committed; `.env.example` documents the variable names without values. This same layering is what a future cloud deployment (Azure App Service/Container Apps, AWS, etc.) would plug into via its own secret store — no code change needed, only where the values come from.

## 10. Clients

**Mobile app (primary, `mobile/`)**: Flutter + Dart, Android and iOS from one codebase. See [`mobile-architecture.md`](mobile-architecture.md) for the full rationale (state management choice, feature-based structure, design system, offline/connectivity considerations relevant to SA users).

**Admin portal (secondary, `admin/`, built later)**: Next.js + TypeScript, internal-only (staff verification review, dispute handling, platform analytics). Deliberately not prioritized before the mobile app's core flows work — it talks to the same REST API, just with `Admin`-policy-gated endpoints (see `api-design.md`).

Both are ordinary REST+OpenAPI clients — the API itself has no concept of "web" or "mobile," so neither client shapes the backend's design.

## 11. Deployment posture (for later, not MVP)

`backend/Dockerfile` builds the API's production image; `docker-compose.yml` covers local development (Postgres + Mailhog + the API — the Flutter app runs on a device/simulator/emulator, not in Compose; the admin portal gets its own Dockerfile once it exists). The explicit non-goal for now is Kubernetes or a service mesh — the modular-monolith boundaries mean that path is available later if one module's load genuinely outgrows the rest, without having designed the domain model around it prematurely.
