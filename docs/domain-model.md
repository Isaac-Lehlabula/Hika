# Domain Model

## 1. Module map

| Module | Core entities |
|---|---|
| Users | `ApplicationUser`, `UserProfile`, `RefreshToken`, `EmailVerificationToken`, `PhoneVerificationCode`, `PasswordResetToken` |
| Drivers | `DriverProfile`, `Vehicle`, `VehiclePhoto` |
| Trips | `Trip`, `TripStop`, `TripSegment`, `Location` |
| Bookings | `Booking`, `BookingPassenger`, `BookingSegment` |
| Payments | `Payment`, `Payout`, `Refund`, `Money` (value object) |
| Reviews | `Review` |
| Notifications | `Notification`, `RideAlert` |
| Trust & Safety | `Verification`, `Report`, `Block`, `EmergencyContact` |
| Admin | `AuditLog` |

`Common`: `Province` (enum, 9 SA provinces), `Money` (owned value object: `Amount decimal(18,2)`, `Currency` — ZAR only for now but modeled as a field, not a constant, so multi-currency is a config change not a schema change), base `Entity`/`AuditableEntity` (Id, CreatedAtUtc, UpdatedAtUtc).

All primary keys are `Guid` (`uuid`), generated client-side (`Guid.CreateVersion7()` where available, for roughly time-ordered IDs that stay index-friendly) rather than relying on DB identity — this keeps entities independently constructible in the domain/application layer before they're persisted, which matters for unit testing and for the domain-event dispatcher.

## 2. Users

- **`ApplicationUser`** — extends `IdentityUser<Guid>` (email, username = email, password hash, lockout fields, security stamp, plus the empty-but-present `AspNetUserLogins` table for future Google/Apple OAuth). Framework-owned; kept thin.
- **`UserProfile`** — 1:1 with `ApplicationUser`. `FirstName`, `LastName`, `PhotoUrl`, `PhoneNumber` (E.164, `+27...`), `PhoneVerifiedAtUtc`, `EmailVerifiedAtUtc`, `MemberSinceUtc`, cached `AverageRating` (decimal, nullable until first review) and `CompletedTripCount` (denormalized counters, updated when a `Review`/`Booking` completes — read far more often than written, so denormalizing here is a deliberate, documented trade-off rather than an oversight).
- **`RefreshToken`** — `UserId`, `TokenHash` (never store the raw token), `ExpiresAtUtc`, `RevokedAtUtc?`, `ReplacedByTokenHash?`, `DeviceInfo` (user agent, coarse), `CreatedByIp`. Rotation: every refresh issues a new token and marks the old one revoked+replaced; reuse of a revoked token revokes the entire chain (theft detection).
- **`EmailVerificationToken`, `PasswordResetToken`** — `UserId`, `TokenHash`, `ExpiresAtUtc`, `UsedAtUtc?`. Same shape, kept as separate tables (not a generic "token" table) because they have different lifetimes/issuance rules and mixing them invites bugs (e.g. a password-reset token accidentally verifying an email).
- **`PhoneVerificationCode`** — `UserId`, `CodeHash`, `ExpiresAtUtc` (short, e.g. 10 min), `AttemptCount` (rate-limits guessing), `UsedAtUtc?`.

A user is never typed as "driver" or "passenger" — anyone can do both. `DriverProfile` existing (and being verified) is what makes someone able to post trips; it doesn't change what kind of `ApplicationUser` they are.

## 3. Drivers & vehicles

- **`DriverProfile`** — 1:1 with `ApplicationUser`. `LicenseNumber`, `LicenseExpiryDate`, `IsVerifiedDriver` (cached from the latest `Verification` of type `DriverLicense`, kept for cheap read-path checks — the `Verification` row is still the source of truth and audit trail).
- **`Vehicle`** — belongs to a `DriverProfile`; a driver can have more than one (`Make`, `Model`, `Year`, `Color`, `RegistrationNumber`, `SeatCapacity`, `VerificationStatus` cached similarly from `Verification` of type `VehicleRegistration`).
- **`VehiclePhoto`** — `VehicleId`, `Url`, `IsPrimary`, `SortOrder`.

## 4. Trips, stops, segments — the core design problem

This is the part of the domain that determines whether the product can actually support "book part of someone's route" without either overselling seats or becoming an unmaintainable mess. It's modeled explicitly rather than as a single origin/destination/seat-count row.

### 4.1 Shape

```
Trip
 ├── TripStop[]      ordered waypoints, Sequence 0..N-1 (origin = 0, destination = N-1)
 ├── TripSegment[]    one row per ADJACENT stop pair — the atomic inventory unit (N-1 rows)
 └── Booking[]        a passenger's reservation for a contiguous sub-range of stops
       └── BookingSegment[]   join rows: exactly which TripSegment(s) this booking consumes
```

- **`Trip`**: `DriverId`, `VehicleId`, `DepartureDateUtc` (+ local display time), `TotalSeatsOffered`, `PricePerSeat` (`Money`), `LuggageAllowance` (free text + optional enum size), `Notes`, `Status` (`Scheduled`/`InProgress`/`Completed`/`Cancelled`). Origin/destination are *not* separate columns — they are simply `TripStop` sequence 0 and sequence N-1; storing them twice would be a denormalization with no benefit here.
- **`TripStop`**: `TripId`, `Sequence` (int, unique per trip), `LocationId?` (FK to the `Location` lookup table), `RawName` (fallback for unlisted villages), `Province`, `EstimatedArrivalUtc?`, `EstimatedDepartureUtc?`. Unique constraint on `(TripId, Sequence)`.
- **`TripSegment`**: `TripId`, `FromStopId`, `ToStopId` (must be adjacent stops — enforced at creation time, not by a DB constraint that can't express "adjacent"), `SeatsAvailable` (int, starts at `Trip.TotalSeatsOffered`, this is the **live, authoritative inventory counter** for that atomic leg), `PriceOverride` (`Money?`, unused in MVP — reserved so future per-segment pricing doesn't require a breaking schema change).

### 4.2 Worked example

Driver posts **Johannesburg → Giyani**, 20 Dec 2026, 05:00, 3 seats, R300/seat, routing through Midrand, Pretoria, Mokopane, Polokwane, Tzaneen:

```
Stops (sequence):
  0 Midrand   1 Pretoria   2 Mokopane   3 Polokwane   4 Tzaneen   5 Giyani

Segments (adjacent pairs, each starts at SeatsAvailable = 3):
  S0: Midrand→Pretoria     S1: Pretoria→Mokopane   S2: Mokopane→Polokwane
  S3: Polokwane→Tzaneen    S4: Tzaneen→Giyani
```

Thabo books **Johannesburg (Midrand) → Polokwane** for 2 seats. That range covers stops 0→3, i.e. segments **S0, S1, S2**. The booking creates:
- `Booking { BoardingStopSeq=0, AlightingStopSeq=3, SeatsRequested=2, Status=Pending }`
- `BookingSegment` rows for S0, S1, S2

Once confirmed, S0/S1/S2 each drop to `SeatsAvailable = 1`; **S3 and S4 are untouched** (still 3 available) — someone can still book Polokwane→Giyani for up to 3 seats. This is exactly the behavior the product brief asks for ("Johannesburg → Polokwane", "Pretoria → Makhado", "Polokwane → Thohoyandou" as independently bookable ranges of the same trip) and it falls out naturally from segment-level inventory rather than needing special-cased logic.

A trip search for "Pretoria → Mokopane, 1 Dec 20" matches any `Trip` where some contiguous stop range starting at-or-after a stop matching "Pretoria" and ending at-or-before a stop matching "Mokopane" has every covered `TripSegment.SeatsAvailable >= requested seats` — a straightforward range query against `TripStop`/`TripSegment`, not a full graph search, because stops on one trip are strictly ordered.

### 4.3 Concurrency-safe seat reservation

Two passengers must never both win the last seat. The reservation path (inside `BookingService.CreateBookingAsync`) runs as a single DB transaction:

1. `SELECT pg_advisory_xact_lock(hashtext(trip_id::text))` — a Postgres advisory lock scoped to this `TripId`, held for the duration of the transaction and released automatically on commit/rollback. This serializes *all* concurrent booking attempts on the same trip, so no request can read stale `SeatsAvailable`.
2. Load the `TripSegment`s covered by the requested stop range.
3. Check `MIN(SeatsAvailable)` across them `>=` requested seat count. If not, fail fast with a 409 Conflict ("not enough seats available") — no partial state is ever written.
4. Decrement `SeatsAvailable` on each covered segment, insert `Booking` (status `Pending`, since MVP requires driver approval — see §5) + `BookingSegment` rows.
5. Commit — lock releases.

**Why a trip-scoped advisory lock instead of row-level `SELECT ... FOR UPDATE` on individual segments**: per-row locking across a variable-length, ordered set of segments needs a fixed lock-acquisition order to avoid deadlocks between two overlapping-but-not-identical bookings (e.g. one booking locking S0→S2 while another locks S1→S3 in the opposite order). A single advisory lock keyed by `TripId` sidesteps that entirely — correctness proof is trivial (only one transaction is ever inside the critical section for a given trip), and the cost is negligible because booking volume *per individual trip* is small (a handful of seats, not thousands). If a specific trip ever became a hot spot, moving to consistently-ordered per-segment row locks is a contained change inside `BookingService`, not a schema change.

**On decline/cancellation**: the reverse — re-acquire the same advisory lock, increment `SeatsAvailable` back on the covered segments, update `Booking.Status`.

### 4.4 Pricing

MVP: one flat `Trip.PricePerSeat` applies regardless of which sub-range is booked (matches every example in the product brief — "R300 per seat" for the whole trip). `TripSegment.PriceOverride` exists but is unused; when/if the product wants distance-based pricing (e.g. Johannesburg→Polokwane cheaper than Johannesburg→Giyani), the segment-sum-of-overrides becomes the price calculation and no migration is needed.

## 5. Bookings

- **`Booking`**: `TripId`, `PassengerUserId`, `BoardingStopId`, `AlightingStopId`, `SeatsRequested`, `Status` (`Pending → Confirmed | Declined`, then `Confirmed → Cancelled | Completed`), `TotalPrice` (`Money`, = seats × trip price at time of booking — captured, not recomputed later, so a later price change doesn't retroactively alter a confirmed booking), `RequestedAtUtc`, `RespondedAtUtc?`, `CancelledAtUtc?`, `CancellationReason?`.
- **`BookingPassenger`**: `BookingId`, `FullName`, `PhoneNumber?`, `IsAccountHolder` (bool — the first row is always the booking user; additional rows are named companions travelling on the same booking, e.g. a parent booking 2 seats for themselves and a child). This exists mainly for the driver's manifest/safety purposes (who is actually in the car), not as a billing unit.
- MVP requires **driver approval** for every booking request (`Pending` state) per the product brief; instant-confirm trips are a natural later addition (a `Trip.RequiresApproval` flag) that doesn't change this model.

## 6. Payments

- **`Payment`**: `BookingId`, `Amount` (`Money`, = fare), `PlatformFee` (`Money`), `DriverPayoutAmount` (`Money`, = Amount − PlatformFee), `Provider` (enum, currently only `Mock`), `ProviderReference?`, `Status` (`Pending`/`Succeeded`/`Failed`/`Refunded`), `CreatedAtUtc`.
- **`Payout`**: `DriverId`, aggregation window or `TripId`, `Amount`, `Status`, `ProviderReference?` — how/when a driver is actually paid out; deliberately decoupled from `Payment` (many payments can roll into one payout run later).
- **`Refund`**: `PaymentId`, `Amount`, `Reason`, `Status`.
- All monetary columns are `decimal(18,2)` at the database level (never `float`/`double`), wrapped in the `Money` value object at the domain level so amount and currency travel together and arithmetic can't silently mix currencies.
- `IPaymentGateway` (Application-layer port): `InitiateChargeAsync`, `RefundAsync`, `GetPayoutStatusAsync`. MVP implementation is `MockPaymentGateway` (auto-succeeds, generates a fake reference) — this is what makes it possible to build and test the entire booking→payment→payout flow before a real South African provider is integrated (see `south-africa.md`).

## 7. Reviews

- **`Review`**: `BookingId`, `ReviewerUserId`, `RevieweeUserId`, `Direction` (`PassengerToDriver`/`DriverToPassenger`), `Rating` (1-5), `Comment?`, `CreatedAtUtc`. Unique constraint on `(BookingId, ReviewerUserId)` — one review per person per booking. **Enforced only when `Booking.Status == Completed`** — this is an application-layer rule (checked in `ReviewService`, tested explicitly), not something a DB constraint can express cleanly, since "completed" is a state transition over time, not a static property of the row being inserted.
- `UserProfile.AverageRating`/`CompletedTripCount` are recomputed (or incrementally updated) when a review is created — read-heavy, write-light, so this denormalization pays for itself immediately on every profile view and trip search result.

## 8. Notifications & ride alerts

- **`Notification`**: `UserId`, `Type` (enum: `BookingRequested`, `BookingAccepted`, `BookingDeclined`, `TripCancelled`, `UpcomingTripReminder`, `PaymentSucceeded`, `NewReview`, ...), `Channel` (`Email`/`Sms`/`Push`/`InApp`), `Payload` (jsonb — channel-specific data), `Status` (`Pending`/`Sent`/`Failed`/`Read`), `CreatedAtUtc`, `ReadAtUtc?`. Every notification is persisted regardless of channel, so the in-app notification bell is just "my `Notification` rows" — email/SMS/push are delivery mechanisms layered on top via `INotificationDispatcher`, not a separate data model.
- **`RideAlert`**: `UserId`, `OriginLocationId?`/`OriginRawText`, `DestinationLocationId?`/`DestinationRawText`, `TravelDate` (or a date range), `Status` (`Active`/`Fulfilled`/`Expired`/`Cancelled`), `CreatedAtUtc`. When a new `Trip` is posted, active alerts whose origin/destination/date are compatible with the new trip's stop range trigger a `Notification`. (Full implementation is Phase 9; the entity is designed now so later phases don't need a schema change.)

## 9. Trust & safety

- **`Verification`**: `SubjectType` (`User`/`Vehicle`), `SubjectId`, `Type` (`IdentityDocument`/`DriverLicense`/`VehicleRegistration`), `Status` (`NotSubmitted`/`Pending`/`Verified`/`Rejected`), `DocumentUrl?`, `SubmittedAtUtc?`, `ReviewedByAdminUserId?`, `ReviewedAtUtc?`, `RejectionReason?`. One table for every kind of verification (rather than a scattered boolean per concept) so a future third-party verification provider (see `south-africa.md`) integrates by writing to one place, and admins have one queue to review.
- **`Report`**: `ReporterUserId`, `ReportedUserId?`, `ReportedTripId?`, `Reason` (enum), `Description`, `Status` (`Open`/`UnderReview`/`Resolved`/`Dismissed`), `CreatedAtUtc`.
- **`Block`**: `BlockerUserId`, `BlockedUserId` — a blocked user's trips are excluded from the blocker's search results and they cannot book each other.
- **`EmergencyContact`**: `UserId`, `Name`, `PhoneNumber`, `Relationship?`. Used for trip-sharing (Phase 10) — sending a trip's live details to a contact — not exposed to other platform users.

## 10. Admin & audit

- **`AuditLog`**: `ActorUserId?` (null = system), `Action` (string, e.g. `"Verification.Approved"`), `EntityType`, `EntityId`, `ChangesJson` (jsonb, before/after where relevant), `CreatedAtUtc`, `IpAddress?`. Written by `IAuditLogger`, called from admin actions and other sensitive operations (verification decisions, refunds, suspensions, fee changes).

## 11. Locations

- **`Location`**: `Name`, `Province`, `Type` (`City`/`Town`/`Township`/`Village`), `Latitude?`, `Longitude?`. Seeded with major SA cities/towns/townships (Johannesburg, Pretoria, Polokwane, Giyani, Thohoyandou, Mbombela, Durban, Cape Town, Mthatha, Nongoma, Rustenburg, and others) to power autocomplete. `TripStop` references `LocationId` when the driver picks a seeded location, but always also stores `RawName`/`Province` so an unlisted village never blocks trip creation — the lookup table is a UX aid, not a constraint on what a trip can say. `ILocationProvider` (Application-layer port) is reserved for a future geocoding/mapping integration; until then, distance-based filtering in search falls back to "same town/province" matching rather than true geo-distance.
