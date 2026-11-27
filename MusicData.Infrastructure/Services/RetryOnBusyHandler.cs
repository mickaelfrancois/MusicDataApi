using System.Net;

namespace MusicData.Infrastructure.Services;

/// <summary>
/// Retries GET requests the provider rejected as busy (503) or throttled (429).
/// MusicBrainz answers 503 "server busy" very often on search; without a retry
/// every such blip surfaces to our clients as a 503.
/// The whole sequence still runs inside the client's HttpClient.Timeout.
/// </summary>
internal sealed class RetryOnBusyHandler(
    int maxRetries,
    Func<TimeSpan, CancellationToken, Task>? delay = null) : DelegatingHandler
{
    internal static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(1500);
    internal static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(5);

    private readonly Func<TimeSpan, CancellationToken, Task> _delay = delay ?? Task.Delay;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (int attempt = 0; ; attempt++)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            bool busy = response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests;
            if (!busy || attempt >= maxRetries || request.Method != HttpMethod.Get)
                return response;

            TimeSpan wait = GetDelay(response, attempt);
            response.Dispose();
            await _delay(wait, cancellationToken);
        }
    }

    // Back off linearly, or longer if the provider's Retry-After asks for it;
    // never wait more than MaxDelay per attempt. Retry-After is a floor only:
    // MusicBrainz's search load-shedding always says "1" yet stays busy for a
    // couple of seconds, so retrying exactly on it mostly fails again.
    private static TimeSpan GetDelay(HttpResponseMessage response, int attempt)
    {
        TimeSpan backoff = BaseDelay * (attempt + 1);
        TimeSpan requested = response.Headers.RetryAfter?.Delta
            ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : TimeSpan.Zero);

        TimeSpan wait = requested > backoff ? requested : backoff;
        return wait > MaxDelay ? MaxDelay : wait;
    }
}
