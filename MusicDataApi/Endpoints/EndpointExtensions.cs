namespace MusicData.Api.Endpoints;

internal static class EndpointExtensions
{
    private const string PublicCacheControl = "public, max-age=30";
    private const string CacheVary = "Accept-Encoding";

    public static RouteHandlerBuilder MapGetCached<TResponse>(
        this RouteGroupBuilder group,
        string pattern,
        Delegate handler)
    {
        return group.MapGet(pattern, handler)
            .Produces<TResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .DisableAntiforgery();
    }

    public static void SetPublicCacheHeaders(this HttpContext context)
    {
        context.Response.Headers.CacheControl = PublicCacheControl;
        context.Response.Headers.Vary = CacheVary;
    }
}
