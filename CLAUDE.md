# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build / Run / Test

Solution file is `MusicDataApi.slnx` (the new XML solution format — use `dotnet` CLI, not `*.sln`).

```bash
rtk dotnet restore MusicDataApi.slnx
rtk dotnet build MusicDataApi.slnx -c Release
rtk dotnet run --project MusicDataApi/MusicData.Api.csproj
rtk dotnet test MusicData.Tests/MusicData.Tests.csproj
rtk dotnet test MusicData.Tests/MusicData.Tests.csproj --filter "FullyQualifiedName~GetArtistByNameTests"   # single class / test
```

Tests live in `MusicData.Tests` (xUnit 2 + NSubstitute + `WebApplicationFactory`), mirroring the layer folders (`Api/E2E`, `Application`, `Infrastructure`, `Shared`). The test project is excluded from the Debug solution build (`Build Project="false"` in the `.slnx`), so build/run it explicitly as above.

E2E tests use `MusicDataApiFactory` (`Api/E2E`): environment `Testing`, in-memory LiteDB (`:memory:`), fixed API key, every external service `Enabled=false`, and `IMusicAggregator` / `ILyricsAggregator` / `IMusicBrainzLookup` replaced with NSubstitute stubs. New E2E tests should go through this factory rather than hitting real providers.

Docker (the `docker-compose.dcproj` is the VS launch profile, real compose lives in the root):

```bash
docker compose up --build           # builds MusicDataApi/Dockerfile
```

The `MusicDataApi.http` file at the repo root contains live request samples for every endpoint — use it to smoke-test instead of writing curl by hand. `docs/audit-2026-05-01.html` is the architecture audit that drove the P0–P2 refactors (commit messages reference its IDs, e.g. "P2-16").

## Target framework

All projects target **net11.0** with `Nullable` and `ImplicitUsings` enabled. Don't downgrade. Package versions are pinned to 11.x — keep them in sync across projects when adding references.

## Architecture

Clean-architecture layout, dependencies flow inward (Api → Application → Domain; Infrastructure → Application/Domain):

- **MusicData.Domain** — Plain POCO entities (`ArtistEntity`, `AlbumEntity`, `LyricsEntity`). No dependencies.
- **MusicData.Application** — Feature handlers (`Features/{Albums,Artists,Lyrics}/Get*.cs`), DTOs, repository/service interfaces, AutoMapper-free hand-written mappers in `Mappers/`.
- **MusicData.Infrastructure** — LiteDB repositories, external HTTP service clients, aggregators, rate limiting, key-based locking, API-key auth. Most types are `internal`; tests reach them via the Api/DI surface.
- **MusicDataApi** (`MusicData.Api.csproj`) — Minimal-API endpoints, `Program.cs` wiring.
- **MusicData.Shared** — Cross-cutting: `Telemetry.cs` (ActivitySource + Meter named `"MusicDataApi"`), `Levenshtein`, request/response contracts.

Each layer has a `ConfigureServices.cs` exposing extension methods on `IServiceCollection` (`AddFeatures`, `AddDataContext`, `AddServices`, `AddTelemetry`, `AddApiAuthentication`, `AddIpRateLimiting`). `Program.cs` is intentionally thin and just calls these.

### Feature handler pattern

Every use case is a sealed class implementing a one-method `IGet*` interface (e.g. `IGetArtistByName.HandleAsync`) and **deriving from `Features/Common/CachedReadHandler<TKey, TDto, TEntity>`**. Handlers are `AddScoped`-registered in `MusicData.Application/ConfigureServices.cs`. New use cases must follow this shape — don't introduce MediatR or a generic dispatcher. Reference: `Features/Artists/GetArtistByName.cs`.

### Cache-aside flow (implemented once, in `CachedReadHandler.HandleAsync`)

Subclasses only supply the hooks (`GetCachedEntity`, `FetchAsync`, `MapToDto`, `Persist`, `MakeLockKey`, optional `IsValid` / `OnNotFound`). The base class does:

1. `IsValid` check, then repository lookup — hit sets `dto.Origin = "Cache"`, `Telemetry.Requests` `result=cache`.
2. On miss, take a per-key lock (`IKeyedLocker`, key like `artist:byname:<lowercased>`), **re-check the cache** (double-checked locking to stop concurrent duplicate external fan-outs), then call the aggregator via `FetchAsync`.
3. Hit: `Persist` (repository `Add`), `result=external`. Miss: `OnNotFound`, `result=not_found`, return null.

Cached rows expire (`LiteDbRepository.TimeToLive`: 30 days; lyrics negative markers 1 day) and older `SchemaVersion` rows count as misses. A DTO with `IsPartial = true` (a provider failed while merging) is returned but **never persisted**. A negative-cache entity must override `IsNegativeHit` so it is answered as null, not mapped to a DTO.

Persistence and cache lookup stay on the **handler**, not inside the aggregator. Don't push caching into the aggregator or the repository. Don't re-implement the flow in a handler — override the hooks.

### Aggregator pattern

`MusicAggregator` and `LyricsAggregator` both derive from `Infrastructure/Services/RateLimitedAggregator<TService>`. Its `RunAllSafeAsync` fans out concurrently to every registered `IMusicService` / `ILyricsService` (`Task.WhenAll`), waits on each service's token bucket first, swallows per-service exceptions (recorded as `result=error` on `musicdata.external_calls`) and returns the non-null results. The aggregators then merge field-by-field with `FirstNonEmpty` / `FirstGreaterThanZero`. The strategy is "first non-empty wins" — order of registration in `Infrastructure/ConfigureServices.cs::AddServices` is therefore a priority list.

External services are registered through the internal helper `AddExternalHttpClient<TInterface, TImpl, TSettings>(configuration, name, "Services:{Name}")` (`Services/HttpClientRegistration.cs`), which binds `IOptions<TSettings>`, sets `BaseAddress`, `Timeout`, gzip/deflate decompression and the `User-Agent: RoK/1.0 (rok@francois.ovh)` header. **If `Services:{Name}:Enabled` is false the service is not registered at all** — the aggregator's `IEnumerable<>` simply doesn't contain it, so implementations carry no per-call "enabled" check.

**Provider contract:** an `IMusicService`/`ILyricsService` returns null **only** for a genuine not-found (HTTP 404, via `HttpResponseExtensions.IsNotFoundOrThrow`) and lets every other failure (non-404 status, timeout, exception) propagate. `RateLimitedAggregator` counts those failures: all-failed-and-empty raises `ExternalServicesUnavailableException` (mapped to 503 + `Retry-After` by `ExternalServicesExceptionHandler`), some-failed marks the merged DTO `IsPartial`. Never swallow errors into null inside a provider — it would be cached as "not found".

`MusicBrainzService` is special: it is exposed as the singleton `IMusicBrainzLookup` (resolved via `OfType<MusicBrainzService>().Single()` from the `IMusicService` list) and used by `MusicAggregator` to resolve names → MBIDs before fanning out. Don't register a second MusicBrainz client; and note that disabling MusicBrainz makes that factory throw (the E2E factory stubs it for this reason).

### Rate limiting (two layers — don't confuse them)

- **Per-external-service token bucket** (`Infrastructure/RateLimiting/TokenBucketRateLimiter.cs`) — created in `RateLimitedAggregator` per service type. Limits come from the `ServiceRateLimits:ServiceLimits` config section (bound to `RateLimitOptions`), keyed by **lowercase type name** (e.g. `"musicbrainzservice"`). When adding a new external service, **also add a key to `appsettings.json`** or it silently falls back to the trivial `(1 per 1ms)` bucket.
- **Per-IP request limiter** (`AspNetCore.RateLimiting`, fixed window) — applied to every endpoint via `.RequireRateLimiting("IpPolicy")`. Rate is `RateLimiting:RequestsPerMinute` from config (default 60). `UseIpRateLimiting()` **must stay before `UseAuthentication()`** so failed API-key attempts are throttled. The partition key is `Connection.RemoteIpAddress`; behind a reverse proxy list it in `ForwardedHeaders:KnownProxies` / `KnownNetworks` (`ForwardedHeadersSetup`) or every client shares the proxy's bucket.

### Authentication

Custom `ApiKeyAuthenticationHandler` reading the `X-Api-Key` header and SHA-256 + `CryptographicOperations.FixedTimeEquals` against `ApiKeySettings:Key`. Every endpoint group uses `.RequireAuthorization("ApiKeyPolicy")`. There is no JWT / OAuth — don't add it without an explicit ask.

### Persistence

LiteDB, single file at `ConnectionStrings:DefaultConnection` (e.g. `/data/database.db` in container; `:memory:` in tests). `ILiteDatabase` is registered as a **singleton** (LiteDB is thread-safe and the file lock is process-scoped — see commit `c187296`, this was a deliberate fix, do not change to scoped). Repositories derive from `Repositories/LiteDbRepository<TEntity>`, are scoped, and expose static `CollectionName` / `EnsureIndexes`; **indexes are created once at startup by the `LiteDbInitializer` hosted service**, not in repository constructors — a new repository/index must be added there. Collections are `"artists"`, `"albums"`, `"lyrics"`. A `LiteDbHealthCheck` is registered with tag `"ready"`.

### Telemetry

OpenTelemetry tracing + metrics, both using the source/meter name `"MusicDataApi"` (declared in `MusicData.Shared/Telemetry/Telemetry.cs`). OTLP HTTP/protobuf exporter is configured **only when `Telemetry:OTEL_EXPORTER_OTLP_ENDPOINT` is set**; New Relic ingestion via `api-key` header from `Telemetry:NEW_RELIC_INSERT_KEY`. Endpoints suffix `/v1/traces` and `/v1/metrics` onto the base — keep that contract when changing the exporter.

Two custom counters: `musicdata.requests` (entity × result, emitted by `CachedReadHandler`) and `musicdata.external_calls` (service × entity × result, emitted by `RateLimitedAggregator`). Endpoints also start a manual `Activity` named after the use case and tag it with the request parameters — replicate that pattern for any new endpoint so dashboards stay consistent.

### Endpoints

All endpoints live under `/v1/...` in `MusicDataApi/Endpoints/`, routed via static `Map*ApiV1` extension methods on `IEndpointRouteBuilder`, and apply both `ApiKeyPolicy` authorization and `IpPolicy` rate limiting at the group level. Each route validates inputs (`Validation.cs`; length caps of 255 / 36 chars) before calling the handler and sets `Cache-Control: public, max-age=30` + `Vary: Accept-Encoding` on success. Health checks are mapped separately at `/health/ready` (filtered by tag `ready`) and `/health/live`.

## Conventions worth respecting

- Sealed handlers and repositories; primary-constructor DI where used.
- Hand-written mappers in `Application/Mappers` — no AutoMapper.
- `Origin` on DTOs (`IOriginAware`) is a free-form string ("Cache", "Aggregated", or service name); don't enum-ify it.
- Credentials are empty placeholders in `appsettings.json`; there is no committed `appsettings.Development.json`. Set real keys with `dotnet user-secrets ... --project MusicDataApi` (`UserSecretsId` is already set on `MusicData.Api.csproj`; see README) — never commit keys.
