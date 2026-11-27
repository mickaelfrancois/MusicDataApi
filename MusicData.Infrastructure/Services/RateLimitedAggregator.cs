using System.Diagnostics;
using Microsoft.Extensions.Options;
using MusicData.Application.Exceptions;
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

    /// <summary>
    /// Outcome of a fan-out: the non-null results plus how many providers
    /// failed (exception or timeout, as opposed to a genuine "not found").
    /// </summary>
    protected readonly record struct RunResult<TResult>(IReadOnlyList<TResult> Items, int Failures)
        where TResult : class
    {
        public bool HasFailures => Failures > 0;

        /// <summary>
        /// Throws when providers failed and nothing came back: "not found"
        /// cannot be concluded and must not be cached.
        /// </summary>
        public IReadOnlyList<TResult> RequireItemsOrNotFound(string entityKind)
        {
            if (Items.Count == 0 && HasFailures)
                throw new ExternalServicesUnavailableException(entityKind);

            return Items;
        }
    }

    protected async Task<RunResult<TResult>> RunAllSafeAsync<TResult>(
        string entityKind,
        Func<TService, CancellationToken, Task<TResult?>> call,
        CancellationToken cancellationToken)
        where TResult : class
    {
        Task<(TResult? Result, bool Failed)>[] tasks = Services
            .Select(s => SafeRunAsync(s, entityKind, call, cancellationToken))
            .ToArray();

        (TResult? Result, bool Failed)[] outcomes = await Task.WhenAll(tasks);
        List<TResult> items = outcomes.Where(o => o.Result is not null).Select(o => o.Result!).ToList();
        return new RunResult<TResult>(items, outcomes.Count(o => o.Failed));
    }


    private async Task<(TResult? Result, bool Failed)> SafeRunAsync<TResult>(
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
            return (result, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            Telemetry.ExternalCalls.Add(1, new TagList
            {
                { "service", serviceName },
                { "entity", entityKind },
                { "result", "error" }
            });
            return (null, true);
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
