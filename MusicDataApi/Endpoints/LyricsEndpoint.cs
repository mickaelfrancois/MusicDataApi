using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Lyrics;
using MusicData.Shared.Telemetry;

namespace MusicData.Api.Endpoints;

public static class LyricsEndpoint
{
    private const int MaxNameLength = 255;

    public static IEndpointRouteBuilder MapLyricsApiV1(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("v1/lyrics")
            .RequireAuthorization("ApiKeyPolicy")
            .RequireRateLimiting("IpPolicy");

        api.MapGetCached<LyricsDto>("/", GetAsync);

        return app;
    }


    public static async Task<IResult> GetAsync(
        [FromQuery] string title,
        [FromQuery] string artistName,
        [FromQuery] string albumName,
        [FromQuery] int duration,
        [FromServices] IGetLyrics getLyrics,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (Validation.RequireMaxLength(title, MaxNameLength, "Title") is { } titleError)
            return titleError;
        if (Validation.RequireMaxLength(artistName, MaxNameLength, "Artist name") is { } artistError)
            return artistError;
        if (Validation.AllowMaxLength(albumName, MaxNameLength, "Album name") is { } albumError)
            return albumError;

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetLyrics", ActivityKind.Server);
        activity?.SetTag("lyrics.title", title);
        activity?.SetTag("lyrics.artist", artistName);
        if (duration > 0)
            activity?.SetTag("lyrics.duration", duration);

        LyricsDto? result = await getLyrics.HandleAsync(title, artistName, albumName, duration, cancellationToken);
        if (result is null)
            return Results.NotFound();

        httpContext.SetPublicCacheHeaders();
        return Results.Ok(result);
    }
}
