# Hiking Spot Admin

Internal staff portal for the Hiking Spot carpooling platform — Next.js 16 (App Router) + TypeScript + Tailwind CSS v4, server-rendered throughout. See `docs/admin-portal.md` at the repo root for the full design writeup.

## Running locally

1. Have the Hika backend running (see `backend/README.md` or the repo root docs) — this app is a thin server-side client of it, not a standalone service.
2. Copy `.env.example` to `.env.local` and point `API_BASE_URL` at your running backend (defaults to `http://localhost:5080`).
3. `npm install && npm run dev` — the app listens on `http://localhost:3000` by default.

## Getting staff access

There's no self-service "become an admin" flow — see `UserProfile.GrantAdmin()`'s remarks in the backend. To grant yourself access locally: register a normal account through the mobile app or `POST /api/v1/auth/register`, then set `Admin__BootstrapEmail` to that email when starting the backend (`dotnet run`). The backend promotes that account to admin on every Development-environment startup if it isn't already.

## Structure

- `src/lib/session.ts` — login/logout Server Actions, cookie handling, the `requireAdminSession()` gate every protected page goes through.
- `src/lib/api.ts` / `src/lib/admin-api.ts` — the authenticated fetch wrapper and typed wrappers for every backend admin endpoint.
- `src/proxy.ts` — Next 16's renamed `middleware.ts`; silently refreshes an expired access token before a protected page renders.
- `src/app/(dashboard)/` — every admin page, grouped so the shared layout (sidebar + auth gate) applies to all of them without adding a URL segment.
- `src/components/` — shared UI: the generic `Table`, `ReasonDialog`/`ConfirmDialog` (used instead of `window.prompt`/`confirm`), filter/pagination controls.

## Scripts

- `npm run dev` — start the dev server
- `npm run build` — production build (also type-checks)
- `npm run lint` — ESLint
