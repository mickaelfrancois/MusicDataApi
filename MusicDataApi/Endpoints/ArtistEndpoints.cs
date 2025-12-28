using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Artists;
using MusicData.Infrastructure.Telemetry;

namespace MusicData.Api.Endpoints;

public static class ArtistEndpoints
{
    public static IEndpointRouteBuilder MapArtistsApiV1(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("v1/artists")
            .RequireAuthorization("ApiKeyPolicy")
            .RequireRateLimiting("IpPolicy");

        api.MapGet("/byName/{artistName}", GetByNameAsync)
            .Produces<ArtistDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        api.MapGet("/byMbid/{musicBrainzId}", GetByMusicBrainzIdAsync)
            .Produces<ArtistDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        return app;
    }


    public static async Task<IResult> GetByNameAsync([FromRoute] string artistName, [FromServices] IGetArtistByName artistByName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(artistName) || artistName.Length > 255)
            return Results.BadRequest("Artist name is required and must be less than 255 characters.");

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetArtistByName", ActivityKind.Server);
        activity?.SetTag("artist.name", artistName);

        ArtistDto? result = await artistByName.HandleAsync(artistName, cancellationToken);

        if (result is null)
            return Results.NotFound(result);

        httpContext.Response.Headers.CacheControl = "public, max-age=30";
        httpContext.Response.Headers.Vary = "Accept-Encoding";

        return Results.Ok(result);
    }

    public static async Task<IResult> GetByMusicBrainzIdAsync([FromRoute] string musicBrainzId, [FromServices] IGetArtistByMusicBrainzId artistByMusicBrainzId, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(musicBrainzId) || musicBrainzId.Length > 36)
            return Results.BadRequest("Artist musicBrainzId is required and must be less than 36 characters.");

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetArtistByMusicBrainzId", ActivityKind.Server);
        activity?.SetTag("artist.musicBrainzId", musicBrainzId);

        ArtistDto? result = await artistByMusicBrainzId.HandleAsync(musicBrainzId, cancellationToken);

        if (result is null)
            return Results.NotFound(result);

        httpContext.Response.Headers.CacheControl = "public, max-age=30";
        httpContext.Response.Headers.Vary = "Accept-Encoding";

        return Results.Ok(result);
    }
}