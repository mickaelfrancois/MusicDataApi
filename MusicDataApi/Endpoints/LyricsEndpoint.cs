using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Lyrics;
using MusicData.Infrastructure.Telemetry;

namespace MusicData.Api.Endpoints;

public static class LyricsEndpoint
{
    public static IEndpointRouteBuilder MapLyricsApiV1(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("v1/lyrics")
            .RequireAuthorization("ApiKeyPolicy")
            .RequireRateLimiting("IpPolicy");

        api.MapGet("/", GetAsync)
                    .DisableAntiforgery();

        return app;
    }


    public static async Task<IResult> GetAsync([FromQuery] string title, [FromQuery] string artistName, [FromQuery] string albumName, [FromQuery] int duration, [FromServices] IGetLyrics getLyrics, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(title) || title.Length > 255)
            return Results.BadRequest("Title is required and must be less than 255 characters.");
        if (string.IsNullOrEmpty(artistName) || artistName.Length > 255)
            return Results.BadRequest("Artist name is required and must be less than 255 characters.");
        if (albumName.Length > 255)
            return Results.BadRequest("Album name must be less than 255 characters.");

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetLyrics", ActivityKind.Server);
        activity?.SetTag("lyrics.title", title);
        activity?.SetTag("lyrics.artist", artistName);
        if (duration > 0)
            activity?.SetTag("lyrics.duration", duration);


        LyricsDto? result = await getLyrics.HandleAsync(title, artistName, albumName, duration, cancellationToken);

        if (result is null)
            return Results.NotFound(result);

        httpContext.Response.Headers.CacheControl = "public, max-age=30";
        httpContext.Response.Headers.Vary = "Accept-Encoding";

        return Results.Ok(result);
    }
}