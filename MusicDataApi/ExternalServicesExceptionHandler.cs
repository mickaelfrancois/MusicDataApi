using Microsoft.AspNetCore.Diagnostics;
using MusicData.Application.Exceptions;

namespace MusicData.Api;

/// <summary>
/// Maps "every provider failed" to 503 + Retry-After instead of a generic 500
/// (or a misleading 404), so clients know to retry later.
/// </summary>
internal sealed class ExternalServicesExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ExternalServicesUnavailableException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        httpContext.Response.Headers.RetryAfter = "30";

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = StatusCodes.Status503ServiceUnavailable, Title = "Upstream music services are unavailable." }
        });
    }
}
