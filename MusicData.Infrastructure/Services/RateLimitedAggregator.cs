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
    /// What one provider answered: a result, a genuine "not found" (null
    /// result, not failed) or a failure (exception or timeout).
    /// </summary>
    protected readonly record struct ServiceOutcome<TResult>(TService Service, TResult? Result, bool Failed)
        where TResult : class;

    /// <summary>
    /// Outcome of a fan-out: one entry per provider, in registration order,
    /// plus the non-null results and how many providers failed.
    /// </summary>
    protected readonly record struct RunResult<TResult>(IReadOnlyList<ServiceOutcome<TResult>> Outcomes)
        where TResult : class
    {
        public IReadOnlyList<TResult> Items { get; } = Outcomes.Where(o => o.Result is not null).Select(o => o.Result!).ToList();

        public int Failures => Outcomes.Count(o => o.Failed);

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
        CancellationToken cancellationToken,
        Func<TService, bool>? only = null)
        where TResult : class
    {
        Task<ServiceOutcome<TResult>>[] tasks = Services
            .Where(s => only is null || only(s))
            .Select(s => SafeRunAsync(s, entityKind, call, cancellationToken))
            .ToArray();

        return new RunResult<TResult>(await Task.WhenAll(tasks));
    }


    /// <summary>
    /// Runs one call the request cannot do without (e.g. the MusicBrainz
    /// name → MBID lookup) outside a fan-out: it waits on the provider's token
    /// bucket like any other call, returns null for a genuine "not found" and
    /// turns a failure into <see cref="ExternalServicesUnavailableException"/>.
    /// </summary>
    protected async Task<TResult?> RunRequiredAsync<TResult>(
        object service,
        string entityKind,
        Func<CancellationToken, Task<TResult?>> call,
        CancellationToken cancellationToken)
        where TResult : class
    {
        string serviceName = service.GetType().Name;

        try
        {
            if (_limiters.TryGetValue(service.GetType(), out TokenBucketRateLimiter? limiter))
                await limiter.WaitForAvailabilityAsync(cancellationToken);

            TResult? result = await call(cancellationToken);
            Telemetry.ExternalCalls.Add(1, new TagList
            {
                { "service", serviceName },
                { "entity", entityKind },
                { "result", result is null ? "not_found" : "ok" }
            });
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Telemetry.ExternalCalls.Add(1, new TagList
            {
                { "service", serviceName },
                { "entity", entityKind },
                { "result", "error" }
            });
            throw new ExternalServicesUnavailableException(entityKind, ex);
        }
    }


    private async Task<ServiceOutcome<TResult>> SafeRunAsync<TResult>(
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
            return new ServiceOutcome<TResult>(service, result, false);
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
            return new ServiceOutcome<TResult>(service, null, true);
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
