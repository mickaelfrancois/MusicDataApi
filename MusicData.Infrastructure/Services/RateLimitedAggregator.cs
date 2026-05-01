using System.Diagnostics;
using Microsoft.Extensions.Options;
using MusicData.Infrastructure.RateLimiting;
using MusicData.Shared.Telemetry;

namespace MusicData.Infrastructure.Services;

internal abstract class RateLimitedAggregator<TService>
    where TService : class
{
    private readonly Dictionary<Type, TokenBucketRateLimiter> _limiters;

    protected IEnumerable<TService> Services { get; }

    protected RateLimitedAggregator(IEnumerable<TService> services, IOptions<RateLimitOptions>? rateLimitOptions)
    {
        Services = services;
        RateLimitOptions limits = rateLimitOptions?.Value ?? new RateLimitOptions();

        _limiters = services
            .Select(s => s.GetType())
            .Distinct()
            .ToDictionary(t => t, t => CreateLimiter(t, limits));
    }

    protected async Task<IReadOnlyList<TResult>> RunAllSafeAsync<TResult>(
        string entityKind,
        Func<TService, CancellationToken, Task<TResult?>> call,
        CancellationToken cancellationToken)
        where TResult : class
    {
        Task<TResult?>[] tasks = Services
            .Select(s => SafeRunAsync(s, entityKind, call, cancellationToken))
            .ToArray();

        TResult?[] results = await Task.WhenAll(tasks);
        return results.Where(r => r is not null).Cast<TResult>().ToList();
    }


    private async Task<TResult?> SafeRunAsync<TResult>(
        TService service,
        string entityKind,
        Func<TService, CancellationToken, Task<TResult?>> call,
        CancellationToken cancellationToken)
        where TResult : class
    {
        string serviceName = service.GetType().Name;

        try
        {
            if (_limiters.TryGetValue(service.GetType(), out TokenBucketRateLimiter? limiter))
                await limiter.WaitForAvailabilityAsync(cancellationToken);

            TResult? result = await call(service, cancellationToken);
            Telemetry.ExternalCalls.Add(1, new TagList
            {
                { "service", serviceName },
                { "entity", entityKind },
                { "result", result is null ? "not_found" : "ok" }
            });
            return result;
        }
        catch (Exception)
        {
            Telemetry.ExternalCalls.Add(1, new TagList
            {
                { "service", serviceName },
                { "entity", entityKind },
                { "result", "error" }
            });
            return null;
        }
    }


    private static TokenBucketRateLimiter CreateLimiter(Type type, RateLimitOptions limits)
    {
        string key = type.Name.ToLowerInvariant();
        if (limits.ServiceLimits.TryGetValue(key, out ServiceRateLimit? cfg) && cfg is not null)
            return new TokenBucketRateLimiter(cfg.MaxRequests, TimeSpan.FromMilliseconds(cfg.PerMilliSeconds));

        return new TokenBucketRateLimiter(1, TimeSpan.FromMilliseconds(1));
    }
}
