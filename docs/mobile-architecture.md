# Mobile Architecture

## 1. Why Flutter

Hika's primary and most-tested use case — a South African on a mid-range Android phone, often on constrained mobile data, planning a trip home for the holidays — is squarely a native-app scenario, not a mobile-web one: it benefits from platform-native performance, push notifications, background-friendly behavior, and an install-and-return usage pattern (search once, get notified when a matching trip appears, come back to book). A single Flutter codebase targeting both Android and iOS is the right trade-off for a small team building a real product: one codebase, one release cadence, near-native performance (Flutter compiles to native ARM code, not a WebView), and a rendering engine that gives full control over the "premium, warm, trustworthy" visual identity the product needs rather than inheriting a platform's default look. React Native was the other realistic candidate; Flutter's edge here is a single language+framework for UI and logic (Dart, no JS bridge), a more consistent cross-platform rendering story (Skia/Impeller draws its own pixels rather than mapping to native widgets, so Android/iOS visual parity is easier to guarantee), and a mature, cohesive tooling story (one package manager, one test framework, one build system).

## 2. State management: Riverpod

Riverpod (not Provider, BLoC, or GetX) for:
- **Compile-time safety** — providers are checked at compile time, not resolved by runtime `BuildContext` lookups the way `package:provider` works; a missing/mistyped provider is a build error, not a runtime crash.
- **Testability** — providers are plain objects that can be overridden in tests without a widget tree, which matters for testing the networking/auth logic that sits under the UI.
- **No `BuildContext` requirement** — business logic (API calls, token refresh, form state) can live outside widgets entirely, keeping the separation described below real rather than aspirational.
- **Right-sized for this app** — BLoC's explicit event/state classes are more ceremony than a marketplace app with fairly standard CRUD+auth flows needs; Riverpod gives structure (providers, notifiers) without that overhead. GetX was considered and rejected — its service-locator/global-state style trades away exactly the compile-time safety and testability Riverpod provides.

Concretely: `Riverpod` (code-generation via `riverpod_generator` for less boilerplate), `AsyncNotifier`/`Notifier` for stateful features (auth session, profile), plain `Provider`/`FutureProvider` for derived/one-shot data (e.g. a trip search result set).

## 3. Project structure

Feature-based, not layer-based-at-the-top — a layer-first structure (`lib/screens/`, `lib/models/`, `lib/services/` each containing every feature's files) becomes unnavigable past a handful of features; feature-first keeps everything about "auth" or "trips" in one place while still separating presentation/state/data *within* each feature.

```
mobile/hika_app/
  lib/
    core/                    App-wide, not feature-specific
      theme/                  Hika design system (colors, type scale, spacing, component themes)
      networking/               Dio client, interceptors (auth header, retry, error mapping)
      routing/                    go_router configuration
      storage/                    Secure token storage wrapper
      error/                       Typed failure/result handling shared across features
    shared/                   Reusable but not app-wide-infrastructure
      widgets/                 Buttons, cards, inputs, badges, empty/loading/error states
      utils/                    Formatters (currency, date), validators mirroring backend rules
    features/
      auth/
        data/                   API client methods + DTOs for this feature
        domain/                  Feature-local models/business rules, if any
        presentation/
          screens/                Register, Login, VerifyEmail, VerifyPhone, ForgotPassword, ResetPassword
          widgets/                 Feature-specific widgets not reused elsewhere
          providers/               Riverpod providers/notifiers for this feature's state
      home/
      search/
      trips/
      bookings/
      profile/
      vehicles/
      notifications/
      safety/
  test/
    features/                 Mirrors lib/features structure
  pubspec.yaml
```

Each feature owns its `data`/`domain`/`presentation` split; `core` holds cross-cutting infrastructure (networking, storage, theme, routing) that every feature depends on but no feature owns. This is intentionally lightweight — no repository-interface-per-feature ceremony, no dependency-injection framework beyond Riverpod's own provider graph. Add structure when a feature actually needs it, not preemptively.

## 4. Networking & the API contract

- **Dio** as the HTTP client (interceptor support for auth headers, retry, and logging is materially better than bare `http`).
- The backend's OpenAPI document (`/openapi/v1.json`) is the source of truth; DTOs are hand-written to start (matching the backend's `Dtos` 1:1) with a generated client (`openapi-generator` or similar) as a later hardening step once the surface stabilizes — premature codegen for a fast-moving Phase 2 API would cost more than it saves right now.
- **Auth header interceptor**: attaches `Authorization: Bearer <access token>` from secure storage to every authenticated request; on a 401, attempts a silent refresh (via `/api/v1/auth/refresh`) once and retries the original request before surfacing an error — this is the mobile equivalent of the web BFF's cookie-refresh dance, just implemented client-side since there's no server-rendered app to host a BFF.
- **Retry**: idempotent GETs get automatic retry-with-backoff on transient network failures (very relevant for the SA-mobile-data scenario this product is built around); mutating requests are not auto-retried unless the endpoint is documented idempotent (see `api-design.md`'s `Idempotency-Key` convention).
- **Pagination**: list screens use the API's `page`/`pageSize`/`totalCount` envelope directly with incremental loading (not "load everything"), keeping payloads small on constrained connections.

## 5. Token storage

Access and refresh tokens are stored via `flutter_secure_storage` (Android Keystore / iOS Keychain-backed) — never `SharedPreferences`/plain files. This is the mobile equivalent of the web architecture's httpOnly-cookie requirement: the goal is the same (don't let tokens sit in something trivially readable), the mechanism differs because there's no browser/XSS threat model on a native app, but there is a "device compromise / other app reading app storage" threat model that secure, OS-backed storage mitigates.

## 6. Design system

A small, centralized Hika theme (`lib/core/theme/`) rather than styling values scattered through widgets:
- **Typography scale**: a handful of named text styles (display, headline, title, body, caption) built on Material 3's type system but with Hika's chosen typeface and weights — not the Material default.
- **Color**: a semantic palette (primary, surface, success, warning, danger, muted text) defined once and referenced everywhere, warm and South-African-feeling rather than a generic blue SaaS palette — exact values are a design decision made when the theme file is built, not hardcoded ad hoc per screen.
- **Spacing/radius**: an 4/8pt-based spacing scale and a small set of corner-radius tokens (cards, buttons, sheets) for visual consistency.
- **Components**: shared `HikaButton`, `HikaCard`, `HikaTextField`, `HikaChip`, `HikaBadge` (for verification/rating badges), loading-skeleton and empty-state widgets — built once in `shared/widgets/`, themed centrally, used everywhere instead of one-off `Container`/`ElevatedButton` styling per screen.
- Material 3 (`useMaterial3: true`) as the base, customized enough (via `ColorScheme`, `TextTheme`, component themes) that it doesn't read as a stock Flutter/Material app — this is a review criterion for every screen built, not a one-time setup task.

## 7. Connectivity & offline-friendliness

Built for the product's real usage pattern — patchy connectivity, especially once a user is near/at a rural destination:
- Cached last-known search results and "my bookings" data (via Riverpod's provider state persisting across navigation, with a lightweight local cache for cross-session persistence added once the relevant features exist) so the app isn't blank on a bad connection.
- Optimistic UI for low-risk actions (e.g., marking a notification read) with rollback on failure; booking/payment actions stay pessimistic (wait for server confirmation) because seat inventory correctness matters more than perceived speed there.
- Clear, specific error states (not just a generic "something went wrong") distinguishing "no connection," "server error," and "you're not allowed to do that" — each with an appropriate retry affordance.

## 8. Navigation

`go_router` for declarative, deep-link-friendly routing (important for links arriving via push notifications and, later, shared trip links) with a simple bottom-navigation shell (Home, Trips, Bookings, Inbox, Profile) — five tabs, matching the product brief's explicit "don't create too many tabs" guidance.

## 9. Platform readiness

- Layouts tested against small Android phones, large Android phones, standard iPhones, and Pro-Max-sized iPhones (`MediaQuery`-driven responsive spacing, no fixed pixel layouts) — tablets are not an MVP priority but nothing should visibly break on one.
- Dark/light mode both defined in the theme from the start (`ThemeMode.system` by default) — retrofitting dark mode later is far more expensive than building it in alongside the light theme.
- Accessibility: semantic labels on interactive elements, sufficient contrast in both theme modes, minimum touch-target sizing — checked per screen, not deferred to a later "accessibility pass."

## 10. Maps & location — kept behind an abstraction

Per the backend's `ILocationProvider` design (`domain-model.md` §11), the mobile app never calls a maps/geocoding SDK directly from feature code — a small `LocationService` abstraction in `core/` wraps whichever provider is integrated (Google Maps, Mapbox — see `south-africa.md`), so picking a different provider later touches one file, not every screen that shows a map or does autocomplete.

## 11. Email verification & password reset links on mobile

The backend emails a link (`{Frontend:BaseUrl}/verify-email?userId=...&token=...`, similarly for password reset — see `domain-model.md` §2). On mobile, the intended long-term handling is a **Universal Link (iOS) / App Link (Android)** so tapping the email link opens the app directly to the right screen with the token pre-filled — this requires domain-verification files (`apple-app-site-association`, Android `assetlinks.json`) hosted at that domain, which is a deployment-time concern, not something to build before there's a real domain. Until then, and as a permanent fallback for "link opened on a different device than the app is installed on," the Verify Email and Reset Password screens also accept the token pasted in manually. This keeps the existing backend token design (already built and tested in Phase 2) fully valid for mobile without inventing a parallel in-app-code verification scheme.

## 12. Push notifications

Planned (not yet built — see `roadmap.md`) around a device-registration flow: on login, the app registers its push token (FCM for Android, APNs via FCM for iOS) against the backend's `Notification`/device-token model, so `INotificationDispatcher` (backend) can target a specific device. The provider (Firebase Cloud Messaging) is the practical default for a Flutter app targeting both platforms; nothing in the backend's notification abstraction is provider-specific, so this is an additive integration, not a redesign.
