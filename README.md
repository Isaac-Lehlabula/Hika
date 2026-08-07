# Hika

**Find your hike home.**

Hika is a South African long-distance carpooling platform built around the local concept of "hiking" — getting a lift with someone already travelling in your direction. It is not an on-demand ride-hailing app: drivers post trips they are already making (typically for holidays, long weekends, and other high-travel periods), and passengers reserve seats on those trips, including on partial segments of a longer route.

The primary product is a **Flutter mobile app** (Android + iOS). A Next.js admin/operations portal is a separate, secondary application built later.

This is a working name and product in active development. See [`/docs`](docs/) for the full architecture, domain model, and roadmap.

## Repository layout

```
backend/    ASP.NET Core (.NET 10) Web API — modular monolith
mobile/     Flutter app (Android + iOS) — the primary customer-facing product
admin/      Next.js admin/operations portal — built later, internal staff only
docs/       Architecture, domain model, API design, and roadmap docs
docker-compose.yml   Local dev stack: Postgres, Mailhog, API
```

## Getting started (local development)

### Prerequisites
- .NET SDK 10
- Flutter SDK (stable channel) with Android/iOS toolchains — `flutter doctor` should report no issues
- Docker Desktop

### 1. Environment
```bash
cp .env.example .env
```
Edit `.env` and set a real `Jwt__SigningKey` for local dev (any long random string is fine).

### 2. Backend
```bash
docker compose up -d postgres mailhog
```
Then run the API — see [`backend/README.md`](backend/README.md).
- API: http://localhost:5080 (OpenAPI/Scalar docs at `/scalar`)
- Mailhog UI (dev email inbox): http://localhost:8025

### 3. Mobile app
See [`mobile/README.md`](mobile/README.md) once the app is scaffolded — `flutter run` against an emulator/simulator/device, pointed at the local API.

## Documentation

Start with [`docs/architecture.md`](docs/architecture.md) for the system overview, [`docs/mobile-architecture.md`](docs/mobile-architecture.md) for the Flutter app, then [`docs/domain-model.md`](docs/domain-model.md) for the core domain (trips, stops, segments, bookings). [`docs/roadmap.md`](docs/roadmap.md) tracks implementation phase status.
