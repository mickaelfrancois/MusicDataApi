# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build / Run / Test

Solution file is `MusicDataApi.slnx` (the new XML solution format — use `dotnet` CLI, not `*.sln`).

```bash
rtk dotnet restore MusicDataApi.slnx
rtk dotnet build MusicDataApi.slnx -c Release
rtk dotnet run --project MusicDataApi/MusicData.Api.csproj
```

There is **no test project** in this solution. Do not invent test commands or claim coverage that doesn't exist.

Docker (the `docker-compose.dcproj` is the VS launch profile, real compose lives in the root):

```bash
docker compose up --build           # builds MusicDataApi/Dockerfile
```

The `MusicDataApi.http` file at the repo root contains live request samples for every endpoint — use it to smoke-test instead of writing curl by hand.

## Target framework

All five projects target **net10.0** with `Nullable` and `ImplicitUsings` enabled. Don't downgrade. Package versions are pinned to 10.x — keep them in sync across projects when adding references.

## Architecture

Clean-architecture layout, dependencies flow inward (Api → Application → Domain; Infrastructure → Application/Domain):

- **MusicData.Domain** — Plain POCO entities (`ArtistEntity`, `AlbumEntity`, `LyricsEntity`). No dependencies.
- **MusicData.Application** — Feature handlers (`Features/{Albums,Artists,Lyrics}/Get*.cs`), DTOs, repository/service interfaces, AutoMapper-free hand-written mappers in `Mappers/`.
- **MusicData.Infrastructure** — LiteDB repositories, external HTTP service clients, aggregators, rate limiting, telemetry, API-key auth.
- **MusicData.Api** — Minimal-API endpoints, `Program.cs` wiring.
- **MusicData.Shared** — Cross-cutting: `Telemetry.cs` (ActivitySource + Meter named `"MusicDataApi"`), `Levenshtein`, request/response contracts.

Each layer has a `ConfigureServices.cs` exposing extension methods on `IServiceCollection` (`AddFeatures`, `AddDataContext`, `AddServices`, `AddTelemetry`, `AddApiAuthentication`, `AddIpRateLimiting`). `Program.cs` is intentionally thin and just calls these.

### Feature handler pattern

Every use case is a sealed class implementing a one-method `IGet*` interface (e.g. `IGetArtistByName.HandleAsync`). Handlers are `AddScoped`-registered in `MusicData.Application/ConfigureServices.cs`. New use cases must follow this shape — don't introduce MediatR or a generic dispatcher.

### Cache-aside flow

Read paths are uniform (see `Features/Artists/GetArtistByName.cs` as the reference):

1. Try repository (LiteDB) — if hit, set `dto.Origin = "Cache"`, increment `Telemetry.Requests` with `result=cache`, return.
2. Else call the matching aggregator (`IMusicAggregator` / `ILyricsAggregator`).
3. On hit: persist via `repository.Add(dto.ToEntity())`, increment `result=external`, return. On miss: increment `result=not_found`, return null.

Persistence and cache lookup happen on the **handler**, not inside the aggregator. Don't push caching into the aggregator or the repository — keep the layering.

### Aggregator pattern

`MusicAggregator` and `LyricsAggregator` fan out concurrent calls to every registered `IMusicService` / `ILyricsService` via `Task.WhenAll`, then merge results field-by-field with `FirstNonEmpty` / `FirstGreaterThanZero` helpers. The strategy is "first non-empty wins" — order of registration in `Infrastructure/ConfigureServices.cs::AddServices` therefore matters as a priority list.

Each external service is registered with `AddHttpClient<TInterface, TImpl>(name, ...)` configuring `BaseAddress`, `Timeout`, gzip/deflate decompression, and a `User-Agent: RoK/1.0 (rok@francois.ovh)` header. Settings come from `Services:{Name}` in `appsettings.json` bound via `IOptions<TSettings>`.

`MusicBrainzService` is special: the aggregator pulls it out of the service list with `OfType<MusicBrainzService>().FirstOrDefault()` to resolve names → MBIDs before fanning out. Don't register a second MusicBrainz client — that lookup assumes a single instance.

### Rate limiting (two layers — don't confuse them)

- **Per-external-service token bucket** (`Infrastructure/RateLimiting/TokenBucketRateLimiter.cs`) — used inside aggregators to respect provider rate limits. Limits are seeded in `AddServices` keyed by lowercase type name (e.g. `"musicbrainzservice"`). When adding a new external service, **also add a key here** or it will silently fall back to the trivial `(1 per 1ms)` bucket.
- **Per-IP request limiter** (`AspNetCore.RateLimiting`, fixed window) — applied to every endpoint via `.RequireRateLimiting("IpPolicy")`. Rate is `RateLimiting:RequestsPerMinute` from config.

### Authentication

Custom `ApiKeyAuthenticationHandler` reading the `X-Api-Key` header and SHA-256 + `CryptographicOperations.FixedTimeEquals` against `ApiKeySettings:Key`. Every endpoint group uses `.RequireAuthorization("ApiKeyPolicy")`. There is no JWT / OAuth — don't add it without an explicit ask.

### Persistence

LiteDB, single file at `ConnectionStrings:DefaultConnection` (e.g. `/data/database.db` in container, a Windows path in dev). `ILiteDatabase` is registered as a **singleton** (LiteDB is thread-safe and the file lock is process-scoped — see commit `c187296`, this was a deliberate fix, do not change to scoped). Repositories are scoped and call `EnsureIndex` in their constructor; collections are `"artists"`, `"albums"`, `"lyrics"`. A `LiteDbHealthCheck` is registered with tag `"ready"`.

### Telemetry

OpenTelemetry tracing + metrics, both using the source/meter name `"MusicDataApi"` (declared in `MusicData.Shared/Telemetry/Telemetry.cs`). OTLP HTTP/protobuf exporter is configured **only when `Telemetry:OTEL_EXPORTER_OTLP_ENDPOINT` is set**; New Relic ingestion via `api-key` header from `Telemetry:NEW_RELIC_INSERT_KEY`. Endpoints suffix `/v1/traces` and `/v1/metrics` onto the base — keep that contract when changing the exporter.

Two custom counters are exposed: `musicdata.requests` (entity × result) and `musicdata.external_calls` (service × entity × result). Endpoints also start a manual `Activity` named after the use case and tag it with the request parameters — replicate that pattern for any new endpoint so dashboards stay consistent.

### Endpoints

All endpoints live under `/v1/...`, are routed via static `Map*ApiV1` extension methods on `IEndpointRouteBuilder`, and apply both `ApiKeyPolicy` authorization and `IpPolicy` rate limiting at the group level. Each route validates inputs (length caps of 255 / 36 chars) before calling the handler and sets `Cache-Control: public, max-age=30` + `Vary: Accept-Encoding` on success. Health checks are mapped separately at `/health/ready` (filtered by tag `ready`) and `/health/live`.

## Conventions worth respecting

- Sealed handlers and repositories; primary-constructor DI where used.
- Hand-written mappers in `Application/Mappers` — no AutoMapper.
- `Origin` on DTOs is a free-form string ("Cache", "Aggregated", or service name); don't enum-ify it.
- `appsettings.Development.json` currently contains real third-party API keys committed to the repo. Don't introduce more secrets there — use user-secrets (`UserSecretsId` is already set on `MusicData.Api.csproj`).
