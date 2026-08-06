# Security

## Authentication & session management

- Passwords hashed via ASP.NET Core Identity's default `PasswordHasher` (PBKDF2, currently HMAC-SHA256, adaptive iteration count) — never a custom scheme.
- JWT access tokens: short-lived (15 min default), signed with a symmetric key for MVP (`Jwt__SigningKey`, ≥32 bytes, from config/secrets — never committed). Documented upgrade path to asymmetric (RS256) signing once there's a reason to let another service verify tokens without holding the signing secret.
- Refresh tokens: opaque random values, **stored hashed** (SHA-256) — a DB leak doesn't hand out usable tokens. Rotated on every use; reuse of an already-rotated token revokes the entire token family (classic theft-detection pattern) and is logged as a security event.
- Lockout: ASP.NET Core Identity's built-in lockout (configurable failed-attempt threshold + cool-down) on login and OTP verification.
- Email/phone verification and password-reset tokens are all hashed at rest and expire quickly (verification links: 24h; OTP codes: 10 min; password reset: 1h).

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

- ASP.NET Core's built-in rate limiter (`Microsoft.AspNetCore.RateLimiting`) applied to auth endpoints (login, OTP request/verify, password reset) — these are the classic brute-force/enumeration targets.
- `forgot-password` always returns 202 regardless of whether the email exists, to avoid account enumeration.
- Report/block endpoints are rate-limited per user to reduce harassment-via-reporting abuse.

## Auditability

- `AuditLog` (see `domain-model.md` §10) records every admin action and other sensitive operation (verification decisions, refunds, suspensions, fee changes) — actor, action, entity, before/after, timestamp, IP. Append-only in normal operation.
- Structured logs (Serilog) include a correlation/trace ID per request, propagated into `ProblemDetails.traceId`, so a user-reported issue can be traced through logs without needing to log PII into the message itself.

## Secrets management

- No secret ever committed — `.env`, `appsettings.*.Secrets.json`, and `*.pfx`/`*.pem` are git-ignored; `.env.example` documents variable *names* only.
- Local dev: `.env` (Docker Compose) or `dotnet user-secrets` (running the API outside Docker).
- Production (future): a real secret store (Azure Key Vault / AWS Secrets Manager / Doppler, depending on eventual hosting choice) injected as environment variables at deploy time — the app code doesn't need to change, only where configuration values come from, because it already reads from `IConfiguration` layered over env vars.

## Known gaps to close before a real launch (tracked, not hidden)

- Real identity/document verification provider (currently: admin manually reviews an uploaded document URL) — see `south-africa.md`.
- Real SMS provider for OTP (currently: logged, not actually sent) — see `south-africa.md`.
- Formal penetration test / dependency vulnerability scanning in CI once there is a CI pipeline (Phase 12).
- Live location sharing (mentioned as a later feature) needs its own explicit privacy design (who can see it, for how long, opt-in) before being built — not scoped into the current phases.
