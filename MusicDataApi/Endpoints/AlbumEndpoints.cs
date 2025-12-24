using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Albums;
using MusicData.Infrastructure.Telemetry;

namespace MusicData.Api.Endpoints;

public static class AlbumEndpoints
{
    public static IEndpointRouteBuilder MapAlbumsApiV1(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder api = app.MapGroup("v1/albums")
            .RequireAuthorization("ApiKeyPolicy")
            .RequireRateLimiting("IpPolicy");

        api.MapGet("/", GetByNameAsync)
                    .DisableAntiforgery();

        return app;
    }


    public static async Task<IResult> GetByNameAsync([FromQuery] string albumName, [FromQuery] string artistName, [FromServices] IGetAlbumByName albumByName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumName) || albumName.Length > 255)
            return Results.BadRequest("Album name is required and must be less than 255 characters.");
        if (string.IsNullOrEmpty(artistName) || artistName.Length > 255)
            return Results.BadRequest("Artist name is required and must be less than 255 characters.");

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetAlbum", ActivityKind.Server);
        activity?.SetTag("album.name", albumName);
        activity?.SetTag("album.artist", artistName);

        AlbumDto? result = await albumByName.HandleAsync(albumName, artistName, cancellationToken);

        if (result is null)
            return Results.NotFound(result);

        httpContext.Response.Headers.CacheControl = "public, max-age=30";
        httpContext.Response.Headers.Vary = "Accept-Encoding";

        return Results.Ok(result);
    }
}