# Hika

**Find your hike home.**

Hika is a South African long-distance carpooling platform built around the local concept of "hiking" — getting a lift with someone already travelling in your direction. It is not an on-demand ride-hailing app: drivers post trips they are already making (typically for holidays, long weekends, and other high-travel periods), and passengers reserve seats on those trips, including on partial segments of a longer route.

This is a working name and product in active development. See [`/docs`](docs/) for the full architecture, domain model, and roadmap.

## Repository layout

```
backend/    ASP.NET Core (.NET 10) Web API — modular monolith
frontend/   Next.js (App Router) web client
docs/       Architecture, domain model, API design, and roadmap docs
docker-compose.yml   Local dev stack: Postgres, Mailhog, API, web
```

## Getting started (local development)

### Prerequisites
- .NET SDK 10
- Node.js 20+
- Docker Desktop

### 1. Environment
```bash
cp .env.example .env
```
Edit `.env` and set a real `Jwt__SigningKey` for local dev (any long random string is fine).

### 2. Run infrastructure only (Postgres + Mailhog), API/web locally for fast iteration
```bash
docker compose up -d postgres mailhog
```
Then run the API and web app from their own directories — see [`backend/README.md`](backend/README.md) and [`frontend/README.md`](frontend/README.md).

### 3. Or run the full stack in Docker
```bash
docker compose up --build
```
- API: http://localhost:5080 (OpenAPI/Scalar docs at `/scalar`)
- Web: http://localhost:3000
- Mailhog UI (dev email inbox): http://localhost:8025

## Documentation

Start with [`docs/architecture.md`](docs/architecture.md) for the system overview, then [`docs/domain-model.md`](docs/domain-model.md) for the core domain (trips, stops, segments, bookings). [`docs/roadmap.md`](docs/roadmap.md) tracks implementation phase status.
