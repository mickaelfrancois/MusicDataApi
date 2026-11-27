namespace MusicData.Api.Endpoints;

internal static class EndpointExtensions
{
    // Responses are API-key authenticated: a shared cache must not store them.
    private const string PrivateCacheControl = "private, max-age=30";
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

    public static void SetPrivateCacheHeaders(this HttpContext context)
    {
        context.Response.Headers.CacheControl = PrivateCacheControl;
        context.Response.Headers.Vary = CacheVary;
    }
}
