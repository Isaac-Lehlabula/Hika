# South African Context & Integrations Requiring Later Investigation

This document flags decisions that need real-world research, legal input, or vendor evaluation before launch. Nothing here is implemented yet beyond the abstraction points that make future integration possible without a redesign.

## Regulatory posture

- **This is cost-sharing carpooling, not e-hailing/metered taxi.** South Africa's National Land Transport Act regulates "e-hailing" (Uber/Bolt-style, on-demand, metered-fare) operators fairly specifically. Hika's model — a driver already making a trip, sharing costs with passengers — is closer to established informal lift-clubs/carpooling than to e-hailing, but **this distinction should be confirmed with SA legal counsel before public launch**, particularly around: whether the platform fee changes the legal characterization from "cost-sharing" to "for-reward transport," provincial operating license requirements, and any requirements specific to long-distance/inter-provincial transport.
- **POPIA (Protection of Personal Information Act)** governs everything here: ID numbers/documents (driver verification), phone numbers, location data, and eventually live-location sharing are all personal information requiring a lawful basis, a privacy policy, data subject access/deletion handling, and breach notification readiness. `docs/security.md`'s PII-minimization approach is a starting point, not a compliance sign-off — a POPIA compliance review is needed before handling real identity documents at scale.
- **Consumer Protection Act** implications for the cancellation-fee/refund flows (`Payment`/`Refund` in the domain model) — cancellation fee terms need to be clearly disclosed and defensible.

## Payment providers to evaluate (behind the existing `IPaymentGateway` abstraction)

South Africa's card/EFT rails differ from the US/EU providers most payment SDKs assume. Candidates to evaluate when a real gateway is needed:
- **PayFast** — widely used by SA SMEs/marketplaces, supports card + EFT + several local wallets, has a marketplace/split-payment style API that could map well to "fare → platform fee + driver payout."
- **Yoco** — strong SA card acquiring, more POS/card-present focused historically but has online APIs.
- **Peach Payments** — SA-focused, marketplace/platform payment support, multiple local payment methods.
- **Ozow** — instant EFT, popular for bank-transfer-preferring users (relevant given not all users will have cards).
- Evaluate on: marketplace/split-payment support (fare vs. platform fee vs. driver payout), payout-to-bank-account capability for drivers, KYC requirements the platform itself must satisfy to move money, pricing, and PCI-DSS scope reduction (hosted fields/redirect vs. handling card data directly — strongly prefer never touching raw card data).

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
