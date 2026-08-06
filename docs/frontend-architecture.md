# Frontend Architecture

## Framework choice: Next.js (App Router) + TypeScript

The brief asked for React/Next.js vs Angular to be decided based on the product's needs. **Next.js**, for:

1. **SEO/discoverability matters here in a way it doesn't for a typical SaaS dashboard.** "Johannesburg to Polokwane lift" or "Durban carpool December" are exactly the kind of searches a real user runs during SA's holiday travel season. Next.js's server rendering means trip-search/route landing pages can actually rank, where a client-only SPA (a typical Angular deployment) mostly can't. This is a genuine product-shaped reason, not a generic framework preference.
2. **Talent pool and future mobile path.** React has a larger SA and global hiring pool than Angular, and if/when Hika builds a native app, React Native lets the team reuse patterns (not code, but component thinking, state-management approach, and in some cases actual business-logic packages) — there's no equivalent bridge from Angular.
3. **Bundle weight and data cost matter for South African mobile users.** Tailwind's compiled CSS is tiny and Next.js ships per-route JS by default; Angular's baseline runtime is heavier. On data-constrained connections this is a real UX difference, not a rounding error.
4. **The API is already framework-agnostic REST+OpenAPI** (see `api-design.md`), so this choice only affects the web client — it doesn't constrain the backend or a future mobile app either way.

Angular would have been a reasonable choice too (its structure and DI actually feel familiar to a .NET developer) — it loses here specifically on the SEO and data-cost points, which matter unusually much for this particular product.

## Stack

- **Next.js 15, App Router**, TypeScript throughout.
- **Tailwind CSS** for styling — utility-first, tiny production CSS, no runtime cost (unlike CSS-in-JS), and directly supports the "minimal, fast, not-enterprise-dashboard" brief.
- **shadcn/ui** (Radix primitives + Tailwind, components copied into the repo rather than an installed black-box library) for accessible base components (dialogs, dropdowns, form controls) that can be freely restyled to a South African-friendly, warm visual identity rather than looking like generic admin-panel chrome.
- **TanStack Query** for server state (search results, trip details, bookings) — caching, refetch-on-focus, optimistic updates for booking actions — instead of hand-rolled `useEffect` fetch logic.
- **React Hook Form + Zod** for forms — matches the backend's FluentValidation rules 1:1 in spirit (schema-defined validation), good mobile UX (inline errors, minimal re-renders).
- Minimal global client state (React Context for the current user only); everything else is server state via TanStack Query or local component state. No Redux — unjustified for this app's shape.

## Project structure

```
frontend/
  app/
    (marketing)/            Public landing/route pages — SSR for SEO
    (auth)/login, register, verify-email, verify-phone, forgot-password, reset-password
    (app)/                  Authenticated app shell
      search/                Find a Hike
      trips/[id]/             Trip detail + reserve seat
      driving/                 "I'm driving" — post/manage trips
      bookings/                 My bookings (as passenger and as driver's incoming requests)
      profile/
    api/auth/[...]/route.ts   BFF route handlers — see "Auth bridging" below
  components/
    ui/                       shadcn/ui primitives
    domain/                   TripCard, SeatPicker, DriverBadge, RatingStars, ...
  lib/
    api-client.ts             Typed fetch wrapper (from OpenAPI-generated types)
    auth.ts                    Session helpers
  hooks/                       TanStack Query hooks per resource (useTrip, useSearchTrips, ...)
```

## Auth bridging (why not just store the JWT in `localStorage`)

Storing a JWT in `localStorage` and attaching it client-side is the common shortcut, and also a real XSS risk (any injected script can read `localStorage` and exfiltrate the token). Instead:

1. The Next.js server (route handlers under `app/api/auth/`) is the only thing that ever calls `/api/v1/auth/login|refresh|logout` on the .NET API directly.
2. On success, the route handler sets the access and refresh tokens as **httpOnly, `Secure`, `SameSite=Lax`** cookies — never readable by client-side JS.
3. Authenticated calls from client components go to a Next.js route handler (`/api/proxy/...`) which reads the httpOnly cookie server-side, attaches `Authorization: Bearer`, and forwards to the .NET API — the browser never sees the raw token.
4. Server Components/Server Actions that need auth read the cookie directly (same process, no extra hop).

This is a small amount of extra plumbing for meaningfully better token security, and it's exactly what "BFF" (backend-for-frontend) pattern is for — a well-known, production-appropriate approach, not a bespoke invention.

## API types

The .NET API's generated OpenAPI document is the source of truth; `openapi-typescript` (or `orval`) generates TypeScript types + a typed client into `frontend/lib/generated/` as a build step, so the frontend can't silently drift from the backend contract. This is wired up once Phase 2's API surface exists (not part of the very first scaffold, which has no endpoints yet).

## Mobile-first UX notes

- Design breakpoints start at 360px width, not 768px — the primary target is a mid-range Android phone on mobile data, not a tablet.
- The passenger home screen is the single "Where are you going home to?" search form described in the brief — origin, destination, date, passenger count, one primary button — with "I'm Driving" as a clearly secondary action, not equal-weighted navigation.
- Images (profile photos, vehicle photos) are served through Next.js's image optimization with aggressive compression — long-distance travel to areas with patchy connectivity is a core scenario, not an edge case.
