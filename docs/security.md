# Security

## Authentication & session management

- Passwords hashed via ASP.NET Core Identity's default `PasswordHasher` (PBKDF2, currently HMAC-SHA256, adaptive iteration count) — never a custom scheme.
- JWT access tokens: short-lived (15 min default), signed with a symmetric key for MVP (`Jwt__SigningKey`, ≥32 bytes, from config/secrets — never committed). Documented upgrade path to asymmetric (RS256) signing once there's a reason to let another service verify tokens without holding the signing secret.
- Refresh tokens: opaque random values, **stored hashed** (SHA-256) — a DB leak doesn't hand out usable tokens. Rotated on every use; reuse of an already-rotated token revokes the entire token family (classic theft-detection pattern) and is logged as a security event.
- Lockout: ASP.NET Core Identity's built-in lockout (configurable failed-attempt threshold + cool-down) on login and OTP verification.
- Email/phone verification and password-reset tokens are all hashed at rest and expire quickly (verification links: 24h; OTP codes: 10 min; password reset: 1h).
- **Client-side token storage**: the Flutter app stores access/refresh tokens in OS-backed secure storage (Android Keystore / iOS Keychain via `flutter_secure_storage`) — never plain preferences/local files. See `mobile-architecture.md` §5. The Next.js admin portal, being a browser app, instead uses httpOnly/`SameSite=Lax` cookies set by Server Actions (never `localStorage`) — every backend call is server-side (a Server Component fetch or a Server Action), so the token never reaches client-side JavaScript at all. See `admin-portal.md`.

## Authorization

- Policy-based authorization (`EmailVerified`, `VerifiedDriver`, `Admin`, see `api-design.md`), never role checks scattered through controller bodies.
- Resource-level checks (e.g. "is this the trip's driver?") live in the application service, not the controller, so the same rule applies however the action is invoked (also relevant once background jobs or admin overrides call the same services).
- Admin endpoints are a distinct policy from day one, not "any authenticated user with a flag checked inline."

## Input validation

- FluentValidation validator per request DTO, run via an action filter before the controller body executes — invalid requests never reach business logic.
- SA phone numbers validated against E.164 `+27` format; free-text fields (trip notes, report descriptions) length-capped and HTML-stripped before storage (defense in depth — the frontend renders as text, never `dangerouslySetInnerHTML`, but stored data shouldn't rely on that alone).
- All EF Core queries are parameterized by construction (LINQ) — no raw string-concatenated SQL. The one place raw SQL appears (advisory locks for seat inventory) uses parameterized `FormattableString`/`Npgsql` parameters, never string interpolation of user input.

## Transport & headers

- HTTPS required in every non-local environment (HSTS enabled); local dev over HTTP is the one accepted exception (Docker Compose on localhost).
- Standard security headers (`X-Content-Type-Options`, `Referrer-Policy`, a CSP scoped to the frontend's own origin plus the API) set at the Next.js/reverse-proxy layer.
- CORS on the API restricted to the known frontend origin(s) — not `*`.

## PII minimization

- Public profile endpoints (`GET /users/{id}`) never return phone number, email, or exact address — only name, photo, rating, verification badges, completed-trip count.
- Pickup/drop-off locations are town/area-level by default; exact addresses (if added later) would only be shared between a driver and a *confirmed* passenger, never in public search results.
- `EmergencyContact` records are never exposed to any user other than their owner (and, for a future live-location-sharing feature, only surfaced automatically in a defined safety flow — not a general API read).

## Rate limiting & abuse prevention

- ASP.NET Core's built-in rate limiter (`Microsoft.AspNetCore.RateLimiting`, see `Hika.Api/RateLimiting/`) applied via two named, configuration-driven policies:
  - `auth` — login, phone-OTP request/verify, and password-reset request/confirm, partitioned by caller IP (all anonymous-accessible), default 10 requests/60s. These are the classic brute-force/enumeration targets.
  - `reports` — filing a report and blocking/unblocking a user, partitioned by the caller's user id (all `[Authorize]`-gated), default 20 requests/60s, to reduce harassment-via-reporting abuse.
  - Limits are `IOptionsMonitor`-backed (`RateLimiting:*` config), not baked in at startup — a rejected request gets a `ProblemDetails`-shaped 429, consistent with every other error response's shape. Integration tests override both to an effectively unlimited value (`CustomWebApplicationFactory`) except one dedicated test class that dials `auth` down to 3/window specifically to exercise the real 429 path.
- `forgot-password` always returns 202 regardless of whether the email exists, to avoid account enumeration.

## Auditability

- `AuditLog` (see `domain-model.md` §10) records every admin action and other sensitive operation (verification decisions, refunds, suspensions, fee changes) — actor, action, entity, before/after, timestamp, IP. Append-only in normal operation.
- Structured logs (Serilog) include a correlation/trace ID per request, propagated into `ProblemDetails.traceId`, so a user-reported issue can be traced through logs without needing to log PII into the message itself.

## Secrets management

- No secret ever committed — `.env`, `appsettings.*.Secrets.json`, and `*.pfx`/`*.pem` are git-ignored; `.env.example` documents variable *names* only.
- Local dev: `.env` (Docker Compose) or `dotnet user-secrets` (running the API outside Docker).
- Production (future): a real secret store (Azure Key Vault / AWS Secrets Manager / Doppler, depending on eventual hosting choice) injected as environment variables at deploy time — the app code doesn't need to change, only where configuration values come from, because it already reads from `IConfiguration` layered over env vars.

## CI & dependency scanning

- `.github/workflows/{backend,mobile,admin}.yml` — build/test (backend: `dotnet test`, including the Testcontainers-backed integration suite; mobile: `flutter analyze` + `flutter test`; admin: `next lint` + `next build`) on every push/PR touching that app, path-filtered so an admin-only change doesn't re-run the backend suite and vice versa.
- `.github/dependabot.yml` — weekly grouped update PRs for NuGet (backend), npm (admin), pub (mobile), and the GitHub Actions themselves. Grouped (one PR per ecosystem per week) rather than one-PR-per-package, since the latter turns into unreviewable noise on a repo this size.
- Both are dormant until the repo has a GitHub remote to run against — see `roadmap.md`.

## Known gaps to close before a real launch (tracked, not hidden)

- Real identity/document verification provider (currently: admin manually reviews an uploaded document URL) — see `south-africa.md`.
- Real SMS provider for OTP (currently: logged, not actually sent) — see `south-africa.md`.
- Real payment gateway (currently: `MockPaymentGateway`, auto-succeeds) — see `south-africa.md` for candidate SA providers (PayFast, Yoco, Peach Payments, Ozow). Needs real merchant credentials this environment doesn't have.
- Formal penetration test — CI now exists (see above) but a scanner still needs to be wired into it (e.g. GitHub code scanning / `dotnet list package --vulnerable` as a CI step) and an actual pentest is a separate, later exercise.
- App-store release prep (signing, store listings, screenshots) — needs Apple/Google developer accounts this environment doesn't have; see `roadmap.md`'s Phase 12 note.
- Live location sharing (mentioned as a later feature) needs its own explicit privacy design (who can see it, for how long, opt-in) before being built — not scoped into the current phases.
