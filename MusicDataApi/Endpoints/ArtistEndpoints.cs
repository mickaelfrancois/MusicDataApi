using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Artists;
using MusicData.Shared.Telemetry;

namespace MusicData.Api.Endpoints;

public static class ArtistEndpoints
{
    private const int MaxNameLength = 255;
    private const int MaxMbidLength = 36;

    public static IEndpointRouteBuilder MapArtistsApiV1(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("v1/artists")
            .RequireAuthorization("ApiKeyPolicy")
            .RequireRateLimiting("IpPolicy");

        api.MapGetCached<ArtistDto>("/byName/{artistName}", GetByNameAsync);
        api.MapGetCached<ArtistDto>("/byMbid/{musicBrainzId}", GetByMusicBrainzIdAsync);

        return app;
    }


    public static async Task<IResult> GetByNameAsync(
        [FromRoute] string artistName,
        [FromServices] IGetArtistByName artistByName,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (Validation.RequireMaxLength(artistName, MaxNameLength, "Artist name") is { } error)
            return error;

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetArtistByName", ActivityKind.Server);
        activity?.SetTag("artist.name", artistName);

        ArtistDto? result = await artistByName.HandleAsync(artistName, cancellationToken);
        if (result is null)
            return Results.NotFound();

        httpContext.SetPublicCacheHeaders();
        return Results.Ok(result);
    }


    public static async Task<IResult> GetByMusicBrainzIdAsync(
        [FromRoute] string musicBrainzId,
        [FromServices] IGetArtistByMusicBrainzId artistByMusicBrainzId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (Validation.RequireMaxLength(musicBrainzId, MaxMbidLength, "Artist musicBrainzId") is { } error)
            return error;

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetArtistByMusicBrainzId", ActivityKind.Server);
        activity?.SetTag("artist.musicBrainzId", musicBrainzId);

        ArtistDto? result = await artistByMusicBrainzId.HandleAsync(musicBrainzId, cancellationToken);
        if (result is null)
            return Results.NotFound();

        httpContext.SetPublicCacheHeaders();
        return Results.Ok(result);
    }
}
