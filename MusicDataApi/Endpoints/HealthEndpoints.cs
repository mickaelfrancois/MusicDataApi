namespace MusicData.Api.Endpoints;

public static class HealthEndpoints2
{
    public static IEndpointRouteBuilder MapHealthEndpoints2(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/live", () =>
            Results.Ok(new { status = "Alive", timestampUtc = DateTime.UtcNow }));

        return app;
    }
}