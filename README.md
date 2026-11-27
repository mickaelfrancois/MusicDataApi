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


### Publishing the image

From an up-to-date `master`, with an active `docker login` on Docker Hub:

```bash
./scripts/docker-publish.sh                    # build + push vladdfpc/musicdataapi:latest
./scripts/docker-publish.sh --version 1.4.0    # also push the 1.4.0 tag
./scripts/docker-publish.sh --no-push          # local build only
DOCKER_IMAGE=me/other ./scripts/docker-publish.sh
```

The script refuses to publish from another branch or with uncommitted changes
(`--force` skips both checks), and labels the image with the commit it was built from.

At runtime the image stores the LiteDB file in `/data` (owned by the non-root app
user): mount a named volume there, and set `ApiKeySettings__Key`. Behind a reverse
proxy such as Traefik, set `ForwardedHeaders__KnownProxies__0` to the proxy's own IP
(give it a fixed one) so the per-IP rate limiter sees real client addresses. Prefer
this to `KnownNetworks`: any container on a trusted network could otherwise forge
`X-Forwarded-For`.

A production compose file for Traefik is in `docs/docker-compose.prod.sample.yml`.
