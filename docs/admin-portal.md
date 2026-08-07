# Admin Portal

A separate Next.js + TypeScript web application (`admin/`) for internal staff, built in Phase 11 once the mobile app's core flows (auth, trips, search, bookings) worked end-to-end, per the original product direction below (kept for context — the reasoning still holds, it's just no longer a future plan).

## Scope

Everything in `api-design.md`'s Admin section: user management (list/search, suspend/unsuspend), the verification review queue (approve/reject driver-license and vehicle-registration submissions), trip oversight (remove), booking oversight (read-only), payment oversight (admin-initiated refund), safety-report resolution (resolve/dismiss), review moderation (delete), a platform-fee setting, an analytics overview, and an audit log of every mutating admin action — all against the same backend used by the mobile app, gated by a new `Admin` authorization policy (see `security.md` and `Hika.Api/Authorization/AdminAuthorizationHandler.cs`).

## Why Next.js here (and not for the customer app)

The reasoning that ruled Next.js out for the *customer* experience (see `mobile-architecture.md` §1 — this is a native-app usage pattern) doesn't apply to an internal admin tool: staff use it on desktops, SEO is irrelevant, and React's ecosystem of admin/data-table/dashboard components made it a fast, sensible choice for an internal tool where "ship a functional CRUD-and-review interface quickly" matters more than a bespoke native feel.

## Shape (as built)

- Next.js 16 App Router + TypeScript + Tailwind CSS v4, server-rendered throughout — every list page is a Server Component doing a server-side fetch against the backend; every mutation is a Server Action (`'use server'`), not a client-side API call. There is no client-side data-fetching library and no client-visible API base URL — `API_BASE_URL` is a server-only env var (see `admin/.env.example`).
- **Auth**: the same backend JWT/refresh model as the mobile app. Login (`app/login`) is a Server Action that calls `/api/v1/auth/login` and stores the access and refresh tokens as httpOnly, sameSite=lax cookies — never `localStorage`, matching the BFF-proxy pattern this doc originally called for. `proxy.ts` (Next 16's renamed `middleware.ts` — see its file-level doc comment) silently refreshes an expired access token using the refresh-token cookie before a protected page renders, since Server Components cannot themselves set cookies (a Next.js constraint, not a design choice). The `(dashboard)/layout.tsx` Server Component then calls a real admin endpoint and distinguishes three outcomes: no/expired session → redirect to `/login`; a valid session that isn't staff → an inline "Not authorized" screen (not a login-loop, since "wrong password" and "not staff" are different problems); authorized → render the sidebar and the requested page.
- **Admin promotion is deliberately not self-service.** There is no "become an admin" endpoint or button anywhere — `UserProfile.GrantAdmin()` is called only from Program.cs's dev-only `Admin:BootstrapEmail` startup step or directly against the database. This is a "trusted small group" model per `api-design.md`'s authorization-policies note; splitting it into finer-grained staff roles is future work if the admin team grows past that.
- **Component approach**: a data-table-heavy UI (a small generic `Table<T>` component, not a third-party library — the tables here are simple enough that pulling in TanStack Table wasn't justified) rather than the consumer app's card-based, mobile-first design language — different product, different UI idioms.
- **Confirmation/reason-collection UI** uses the native `<dialog>` element (`ReasonDialog`/`ConfirmDialog` components) rather than `window.prompt`/`window.confirm` — both better UX (styleable, doesn't block on browser chrome) and a hard requirement in practice, since at least one preview/testing environment used during development doesn't support blocking native dialogs at all.
- **Server Action argument-passing**: row-scoped actions (suspend *this* user, reject *this* verification) pass their target id via `action.bind(null, id)` on the imported Server Action — the documented Next.js pattern for parameterizing a Server Action bound to a specific list row — rather than wrapping it in a new arrow function, which Next.js rejects when passed from a Server Component into a Client Component (only a Server Action reference, bound or not, can cross that boundary).

## Deliberately out of scope (for now)

- Finer-grained staff roles (read-only support vs. full admin) — noted above as future work once the "trusted small group" model stops fitting.
- A payout-run trigger — the `Payout` entity itself remains unbuilt (see `domain-model.md` §6 and `roadmap.md`); the admin portal has nowhere to trigger something that doesn't exist yet.
- CI/deployment for the admin app specifically — tracked under Phase 12 (Production hardening) alongside the backend and mobile app's CI.
