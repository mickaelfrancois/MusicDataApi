using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Albums;
using MusicData.Application.Features.Artists;
using MusicData.Shared.Telemetry;

namespace MusicData.Api.Endpoints;

public static class AlbumEndpoints
{
    private const int MaxNameLength = 255;
    private const int MaxMbidLength = 36;

    public static IEndpointRouteBuilder MapAlbumsApiV1(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("v1/albums")
            .RequireAuthorization("ApiKeyPolicy")
            .RequireRateLimiting("IpPolicy");

        api.MapGetCached<AlbumDto>("/byName/{artistName}/{albumName}", GetByNameAsync);
        api.MapGetCached<AlbumDto>("/byMbid/{artistMusicBrainzId}/{albumMusicBrainzId}", GetByMusicBrainzIdAsync);

        return app;
    }


    public static async Task<IResult> GetByNameAsync(
        [FromRoute] string artistName,
        [FromRoute] string albumName,
        [FromServices] IGetArtistByName artistByName,
        [FromServices] IGetAlbumByName albumByName,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (Validation.RequireMaxLength(albumName, MaxNameLength, "Album name") is { } albumError)
            return albumError;
        if (Validation.RequireMaxLength(artistName, MaxNameLength, "Artist name") is { } artistError)
            return artistError;

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetAlbumByName", ActivityKind.Server);
        activity?.SetTag("album.name", albumName);
        activity?.SetTag("album.artist", artistName);

        ArtistDto? artist = await artistByName.HandleAsync(artistName, cancellationToken);
        if (artist?.MusicBrainzID is null)
            return Results.NotFound();

        AlbumDto? result = await albumByName.HandleAsync(albumName, artist.MusicBrainzID, cancellationToken);
        if (result is null)
            return Results.NotFound();

        httpContext.SetPublicCacheHeaders();
        return Results.Ok(result);
    }


    public static async Task<IResult> GetByMusicBrainzIdAsync(
        [FromRoute] string artistMusicBrainzId,
        [FromRoute] string albumMusicBrainzId,
        [FromServices] IGetAlbumByMusicBrainzId albumByMusicBrainzId,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (Validation.RequireMaxLength(artistMusicBrainzId, MaxMbidLength, "Artist musicBrainzId") is { } artistError)
            return artistError;
        if (Validation.RequireMaxLength(albumMusicBrainzId, MaxMbidLength, "Album musicBrainzId") is { } albumError)
            return albumError;

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetAlbumByMusicBrainzId", ActivityKind.Server);
        activity?.SetTag("album.musicBrainzId", albumMusicBrainzId);
        activity?.SetTag("album.artistMusicBrainzId", artistMusicBrainzId);

        AlbumDto? result = await albumByMusicBrainzId.HandleAsync(albumMusicBrainzId, artistMusicBrainzId, cancellationToken);
        if (result is null)
            return Results.NotFound();

        httpContext.SetPublicCacheHeaders();
        return Results.Ok(result);
    }
}
