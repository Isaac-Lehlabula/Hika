# South African Context & Integrations Requiring Later Investigation

This document flags decisions that need real-world research, legal input, or vendor evaluation before launch. Nothing here is implemented yet beyond the abstraction points that make future integration possible without a redesign.

## Regulatory posture

- **This is cost-sharing carpooling, not e-hailing/metered taxi.** South Africa's National Land Transport Act regulates "e-hailing" (Uber/Bolt-style, on-demand, metered-fare) operators fairly specifically. Hika's model — a driver already making a trip, sharing costs with passengers — is closer to established informal lift-clubs/carpooling than to e-hailing, but **this distinction should be confirmed with SA legal counsel before public launch**, particularly around: whether the platform fee changes the legal characterization from "cost-sharing" to "for-reward transport," provincial operating license requirements, and any requirements specific to long-distance/inter-provincial transport.
- **POPIA (Protection of Personal Information Act)** governs everything here: ID numbers/documents (driver verification), phone numbers, location data, and eventually live-location sharing are all personal information requiring a lawful basis, a privacy policy, data subject access/deletion handling, and breach notification readiness. `docs/security.md`'s PII-minimization approach is a starting point, not a compliance sign-off — a POPIA compliance review is needed before handling real identity documents at scale.
- **Consumer Protection Act** implications for the cancellation-fee/refund flows (`Payment`/`Refund` in the domain model) — cancellation fee terms need to be clearly disclosed and defensible.

## Payment provider (implemented: Ozow)

**Ozow** — a South African instant-EFT, redirect-based payment gateway — is implemented behind `IPaymentGateway` (`OzowPaymentGateway` in `Hika.Infrastructure/Payments/Ozow`). Unlike the `MockPaymentGateway` it replaces, Ozow never settles synchronously: `InitiatePaymentAsync` posts a signed request to Ozow's API and returns a hosted-payment-page `RedirectUrl`; the actual outcome arrives later via a hash-verified webhook (`OzowWebhooksController`, `POST /api/v1/webhooks/ozow/{payment,refund}-notify`). A new `AwaitingPayment` booking state sits between the driver accepting a request and the booking being `Confirmed`/`Declined`, so a passenger is only ever asked to pay once a driver has actually accepted — see `docs/domain-model.md`/`Booking.cs` for the full state machine.

**Not yet live-verified.** No Ozow sandbox credentials were available while building this — `OzowOptions` (SiteCode/PrivateKey/ApiKey) ship as empty config placeholders, and the app falls back to `MockPaymentGateway` until `Ozow:SiteCode` is set (see `DependencyInjection.cs`). Two things specifically need confirming against a real Ozow merchant account before going live, both flagged in code comments at their call sites:
- **HashCheck field order** (`OzowPaymentGateway`, `OzowHashHelper`) — the SHA-512-over-concatenated-values algorithm is well-documented, but the *exact* order of fields going into that concatenation was reconstructed from third-party reference implementations, not Ozow's own merchant-portal integration guide.
- **Refund endpoint path/field names** (`/postrefund`) — Ozow's refund product requires separate merchant enrollment; the request shape here is a best-effort guess.

A mismatch in either fails closed (Ozow rejects the signed request, or our own `OzowNotifyVerifier` rejects an incoming webhook) rather than silently misprocessing a payment — but both should be checked against Merchant Admin → Integration on ozow.com once real credentials exist.

Other SA providers considered but not built (kept here for reference if Ozow needs to be swapped or supplemented later, e.g. for card payments):
- **PayFast** — widely used by SA SMEs/marketplaces, supports card + EFT + several local wallets, has a marketplace/split-payment style API that could map well to "fare → platform fee + driver payout."
- **Yoco** — strong SA card acquiring, more POS/card-present focused historically but has online APIs.
- **Peach Payments** — SA-focused, marketplace/platform payment support, multiple local payment methods.

## Identity/document verification providers to evaluate (behind the existing `Verification` entity + admin review queue)

- **Smile ID** — pan-African identity verification (ID document + selfie liveness), explicit South African ID/driver's license support.
- **Trulioo** — global identity verification network with SA coverage.
- Home Affairs-adjacent verification services (where accessible via an approved reseller) for ID number/document authenticity checks.
- Any provider chosen should replace the MVP's manual-admin-review flow *underneath* the existing `Verification` entity/status model, not require a new one — `Status` transitions (`Pending → Verified/Rejected`) already fit an automated provider callback as easily as a human reviewer.

## SMS/OTP providers to evaluate (behind the existing `ISmsSender` interface)

- **Clickatell** — South African-founded, strong local delivery rates.
- **BulkSMS** — SA-based, simple API, commonly used by SA platforms.
- **Infobip / Twilio** — global providers with SA number support, useful if international expansion is ever considered.
- Evaluate on: delivery reliability to SA mobile networks specifically (local providers often outperform global ones here), cost per SMS at expected OTP volume, and support for SA's major networks (Vodacom, MTN, Cell C, Telkom).

## Mapping/geocoding providers to evaluate (behind the existing `ILocationProvider` interface)

- **Google Maps Platform** — best SA coverage including smaller towns, but usage-based cost at scale.
- **Mapbox** — competitive alternative, worth comparing on SA-specific geocoding accuracy for townships/villages, which is exactly where this product needs strong coverage (not just metro CBDs).
- Whichever is chosen, budget for the fact that many relevant destinations (villages outside Giyani, Mthatha, Thohoyandou, Nongoma, etc.) are exactly the addresses global geocoders handle worst — the `Location` seed table + free-text fallback (already in the domain model) is intentionally not fully dependent on geocoding quality.

## Travel-pattern context that should inform product decisions later (not architecture)

- Holiday travel is extremely seasonal (December, Easter, month-end weekends, university term breaks) — expect large demand spikes the platform's data model already handles (it's just more `Trip`/`Booking` rows) but which operations/support and payment-provider rate limits should be sized for.
- Luggage volume for long-distance/relocation-style trips (students, holiday travel with gifts) is meaningfully higher than a city commute — already reflected in `Trip.LuggageAllowance`, worth revisiting with real user feedback.
- Connectivity is not uniform — rural destinations may have limited data; this is why the frontend architecture prioritizes small payloads and SSR over a heavy client-side SPA experience.
