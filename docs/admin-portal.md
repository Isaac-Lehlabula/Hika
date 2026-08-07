# Admin Portal (deferred)

A separate Next.js + TypeScript web application (`admin/`) for internal staff — not built until the mobile app's core flows (auth, trips, search, bookings) work end-to-end, per explicit product direction. Notes here capture the intended shape so the decision isn't lost, not a commitment to build it next.

## Scope

Everything in `api-design.md`'s Admin section: user/driver/vehicle management, verification review queue, trip and booking oversight, payments/refunds, safety reports, review moderation, platform fee configuration, popular-route/analytics viewing, and the audit log — all `Admin`-policy-gated endpoints on the same backend used by the mobile app.

## Why Next.js here (and not for the customer app)

The reasoning that ruled Next.js out for the *customer* experience (see `mobile-architecture.md` §1 — this is a native-app usage pattern) doesn't apply to an internal admin tool: staff use it on desktops, SEO is irrelevant, and React's ecosystem of admin/data-table/dashboard components (and the team's familiarity with the stack from evaluating it earlier) make it a fast, sensible choice for an internal tool where "ship a functional CRUD-and-review interface quickly" matters more than a bespoke native feel.

## Shape (when built)

- Standard Next.js App Router + TypeScript, server-rendered where it helps (data tables, forms), no BFF-cookie complexity needed the way a public customer app would (an internal tool behind staff auth has a smaller, differently-shaped threat model — still never storing tokens in `localStorage`, but the httpOnly-cookie-proxy pattern documented for a hypothetical public web client applies here too if built).
- Auth: same backend JWT/refresh model, staff accounts gated by the `Admin` authorization policy (see `security.md`); consider requiring a stronger auth step (e.g. shorter session lifetime, or a future 2FA requirement) given the sensitivity of what it can do (refunds, suspensions, verification approval).
- Component approach: a data-table-heavy UI (TanStack Table or similar) rather than the consumer app's card-based, mobile-first design language — different product, different UI idioms are appropriate.
