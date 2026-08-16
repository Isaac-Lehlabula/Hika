# Hiking Spot mobile app (Flutter)

The primary customer-facing product — Android + iOS from one codebase. See [`/docs/mobile-architecture.md`](../../docs/mobile-architecture.md) for the full design rationale (Riverpod choice, feature-based structure, design system, token storage).

## Structure

```
lib/
  core/           App-wide infrastructure: theme, networking (Dio + auth interceptor),
                  secure token storage, go_router config, environment config
  shared/         Reusable widgets (HikaButton, HikaTextField, HikaCard, HikaEmptyState)
  features/
    auth/           Register, login, email/phone verification, password reset
    profile/        Own profile view/edit
    drivers/        Become-a-driver flow, vehicle management, photo/document upload
    home/           The flagship "Where are you going home to?" search screen
    shell/          Bottom-nav shell, splash screen
test/             Mirrors lib/ structure
```

## Running locally

1. Start the backend: from the repo root, `docker compose up -d postgres mailhog`, then `dotnet run --project backend/src/Hika.Api` (see [`/backend/README.md`](../../backend/README.md)).
2. From this directory: `flutter pub get`
3. Run on a device/emulator/simulator: `flutter run`
   - Android emulator and iOS simulator both resolve the API's local address automatically (`AppConfig` in `lib/core/config/app_config.dart` handles the Android-emulator-vs-`10.0.2.2` quirk).
   - Override the API base URL for a physical device or staging: `flutter run --dart-define=API_BASE_URL=http://<your-machine-ip>:5080`
   - `flutter run -d chrome` also works for quick UI iteration in a browser — useful for fast feedback, but the shipped product targets Android/iOS, not web.

## Testing

```bash
flutter analyze
flutter test
```

## Current status

Auth flow (register, login, logout, email verification, phone OTP verification, forgot/reset password), profile view/edit, and driver onboarding (license details + document submission, vehicle CRUD, photo upload with a primary-photo picker, vehicle registration-document submission) are implemented and wired to the real backend. The bottom-nav shell (Home, Trips, Bookings, Inbox, Profile) is in place; Home shows the flagship search UI (not yet wired to a backend — that's Phase 5); Trips/Bookings/Inbox show honest "coming soon" states. See [`/docs/roadmap.md`](../../docs/roadmap.md).
