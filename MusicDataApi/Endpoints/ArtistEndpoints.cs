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

        api.MapGet("/", GetByNameAsync)
                    .DisableAntiforgery();

        return app;
    }


    public static async Task<IResult> GetByNameAsync([FromQuery] string artistName, [FromServices] IGetArtistByName artistByName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(artistName) || artistName.Length > 255)
            return Results.BadRequest("Artist name is required and must be less than 255 characters.");

        using Activity? activity = Telemetry.ActivitySource.StartActivity("GetArtist", ActivityKind.Server);
        activity?.SetTag("artist.name", artistName);

        ArtistDto? result = await artistByName.HandleAsync(artistName, cancellationToken);

        if (result is null)
            return Results.NotFound(result);

        httpContext.Response.Headers.CacheControl = "public, max-age=30";
        httpContext.Response.Headers.Vary = "Accept-Encoding";

        return Results.Ok(result);
    }
}