# Hiking Spot API (backend)

ASP.NET Core (.NET 10) Web API, modular monolith. See [`/docs/architecture.md`](../docs/architecture.md) for the full design rationale.

## Projects

```
src/Hika.Domain          Entities, value objects, domain events — no external dependencies
src/Hika.Application     Use-case services, DTOs, validators, port interfaces
src/Hika.Infrastructure  EF Core, Identity, external service adapters
src/Hika.Api             Controllers, DI composition, middleware, OpenAPI
tests/Hika.UnitTests         xUnit + Shouldly + NSubstitute — no I/O
tests/Hika.IntegrationTests  xUnit + Shouldly + Testcontainers.PostgreSql + WebApplicationFactory
```

## Running locally

1. Start Postgres + Mailhog from the repo root: `docker compose up -d postgres mailhog` (copy `.env.example` to `.env` first).
2. From `backend/`: `dotnet run --project src/Hika.Api`
3. API: http://localhost:5080 (port from `Properties/launchSettings.json`, or pass `--urls`)
   - Interactive docs: `/scalar`
   - OpenAPI document: `/openapi/v1.json`
   - Health: `/health/live`, `/health/ready`

The API auto-applies EF Core migrations on startup **in the Development environment only** — never in other environments, where migrations are expected to run as an explicit, separate step.

## Testing

```bash
dotnet test
```

Integration tests spin up a real Postgres container via Testcontainers — Docker must be running.

## Migrations

```bash
dotnet ef migrations add <Name> --project src/Hika.Infrastructure --startup-project src/Hika.Api
dotnet ef database update --project src/Hika.Infrastructure --startup-project src/Hika.Api
```

## Configuration

Layered per standard ASP.NET Core convention: `appsettings.json` → `appsettings.{Environment}.json` → environment variables → `dotnet user-secrets` (local dev only). Never commit real secrets — see [`/docs/security.md`](../docs/security.md).
