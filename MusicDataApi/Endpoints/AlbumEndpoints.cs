using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Albums;
using MusicData.Application.Features.Artists;
using MusicData.Infrastructure.Telemetry;

namespace MusicData.Api.Endpoints;

public static class AlbumEndpoints
{
    public static IEndpointRouteBuilder MapAlbumsApiV1(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("v1/albums")
            .RequireAuthorization("ApiKeyPolicy")
            .RequireRateLimiting("IpPolicy");

        api.MapGet("/byName/{artistName}/{albumName}", GetByNameAsync)
                    .Produces<ArtistDto>(StatusCodes.Status200OK)
                    .Produces(StatusCodes.Status400BadRequest)
                    .Produces(StatusCodes.Status404NotFound)
                    .DisableAntiforgery();

        api.MapGet("/byMbid/{artistMusicBrainzId}/{albumMusicBrainzId}", GetByMusicBrainzIdAsync)
            .Produces<ArtistDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        return app;
    }


    public static async Task<IResult> GetByNameAsync([FromRoute] string artistName, [FromRoute] string albumName, [FromServices] IGetArtistByName artistByName, [FromServices] IGetAlbumByName albumByName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumName) || albumName.Length > 255)
            return Results.BadRequest("Album name is required and must be less than 255 characters.");
        if (string.IsNullOrEmpty(artistName) || artistName.Length > 255)
            return Results.BadRequest("Artist name is required and must be less than 255 characters.");

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetAlbumByName", ActivityKind.Server);
        activity?.SetTag("album.name", albumName);
        activity?.SetTag("album.artist", artistName);

        ArtistDto? artist = await artistByName.HandleAsync(artistName, cancellationToken);
        if (artist?.MusicBrainzID is null)
            return Results.NotFound();

        AlbumDto? result = await albumByName.HandleAsync(albumName, artist.MusicBrainzID, cancellationToken);
        if (result is null)
            return Results.NotFound();

        httpContext.Response.Headers.CacheControl = "public, max-age=30";
        httpContext.Response.Headers.Vary = "Accept-Encoding";

        return Results.Ok(result);
    }


    public static async Task<IResult> GetByMusicBrainzIdAsync([FromRoute] string artistMusicBrainzId, [FromRoute] string albumMusicBrainzId, [FromServices] IGetAlbumByMusicBrainzId albumByMusicBrainzId, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(artistMusicBrainzId) || artistMusicBrainzId.Length > 36)
            return Results.BadRequest("Artist musicBrainzId is required and must be less than 36 characters.");
        if (string.IsNullOrEmpty(albumMusicBrainzId) || albumMusicBrainzId.Length > 36)
            return Results.BadRequest("Album musicBrainzId is required and must be less than 36 characters.");

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetByMusicBrainzIdAsync", ActivityKind.Server);
        activity?.SetTag("album.musicBrainzId", albumMusicBrainzId);
        activity?.SetTag("album.artistMusicBrainzId", artistMusicBrainzId);

        AlbumDto? result = await albumByMusicBrainzId.HandleAsync(albumMusicBrainzId, artistMusicBrainzId, cancellationToken);

        if (result is null)
            return Results.NotFound(result);

        httpContext.Response.Headers.CacheControl = "public, max-age=30";
        httpContext.Response.Headers.Vary = "Accept-Encoding";

        return Results.Ok(result);
    }
}