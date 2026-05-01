# MusicDataApi

ASP.NET Core minimal-API that aggregates artist, album and lyrics data from
multiple external services (MusicBrainz, LastFm, Fanart.tv, CoverArtArchive,
LyricsOvh, LrcLib) and caches the result in a local LiteDB file.

See `docs/audit-2026-05-01.html` for the architecture audit and
`CLAUDE.md` for the architectural cheatsheet used by Claude Code sessions.

## Local setup

Prerequisites: **.NET 10 SDK**.

The repository ships with empty placeholders for every credential. Populate
them via [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
— never commit a real key to `appsettings.Development.json`.

The User Secrets ID is already configured on `MusicData.Api.csproj`, so from
the repo root:

```bash
dotnet user-secrets set "ApiKeySettings:Key"            "<x-api-key clients must send>" --project MusicDataApi
dotnet user-secrets set "Services:LastFM:ApiKey"        "<your last.fm api key>"        --project MusicDataApi
dotnet user-secrets set "Services:FanArt:ApiKey"        "<your fanart.tv api key>"      --project MusicDataApi
dotnet user-secrets set "Telemetry:NEW_RELIC_INSERT_KEY" "<your new relic insert key>"  --project MusicDataApi
```

Secrets live under `%APPDATA%/Microsoft/UserSecrets/<UserSecretsId>/secrets.json`
on Windows and never enter the repository.

For the `MusicDataApi.http` smoke-test file, replace the `@ApiKey` placeholder
with your local value (or use the `httpYac` / `REST Client` env-file mechanism
of your editor).

## Running

```bash
dotnet build MusicDataApi.slnx -c Release
dotnet run --project MusicDataApi/MusicData.Api.csproj
```

Health checks:

- `GET /health/live` — process is up
- `GET /health/ready` — LiteDB reachable

OpenAPI is exposed in development at `/openapi/v1.json`.

## Tests

```bash
dotnet test MusicData.Tests/MusicData.Tests.csproj
```

## Container

```bash
docker compose up --build
```

Mounts `%APPDATA%/Microsoft/UserSecrets` read-only into the container so
user-secrets work the same way locally and in the dev container.
