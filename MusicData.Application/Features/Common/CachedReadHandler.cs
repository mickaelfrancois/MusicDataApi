using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Shared.Telemetry;

namespace MusicData.Application.Features.Common;

public abstract class CachedReadHandler<TKey, TDto, TEntity>
    where TKey : notnull
    where TDto : class, IOriginAware
    where TEntity : class
{
    private readonly IKeyedLocker _locker;
    private readonly ILogger _logger;

    protected CachedReadHandler(IKeyedLocker locker, ILogger logger)
    {
        _locker = locker;
        _logger = logger;
    }

    protected abstract string EntityKind { get; }

    protected abstract TEntity? GetCachedEntity(TKey key);

    protected abstract Task<TDto?> FetchAsync(TKey key, CancellationToken cancellationToken);

    protected abstract TDto MapToDto(TEntity entity);

    protected abstract void Persist(TDto dto);

    protected abstract string MakeLockKey(TKey key);

    protected virtual bool IsValid(TKey key) => true;

    protected virtual void OnNotFound(TKey key) { }

    /// <summary>
    /// True when the cached row is a negative-cache marker ("known missing"):
    /// the handler then answers null without calling any external service.
    /// </summary>
    protected virtual bool IsNegativeHit(TEntity entity) => false;

    public async Task<TDto?> HandleAsync(TKey key, CancellationToken cancellationToken = default)
    {
        if (!IsValid(key))
            return null;

        if (TryGetCached(key, out TDto? cached))
            return cached;

        using IDisposable _ = await _locker.LockAsync(MakeLockKey(key), cancellationToken);

        if (TryGetCached(key, out cached))
            return cached;

        TDto? fetched = await FetchAsync(key, cancellationToken);
        if (fetched is null)
        {
            _logger.LogInformation("{Entity} '{Key}' not found in any music service.", EntityKind, key);
            Telemetry.Requests.Add(1, new TagList { { "entity", EntityKind }, { "result", "not_found" } });
            OnNotFound(key);
            return null;
        }

        if (fetched.IsPartial)
        {
            _logger.LogWarning("{Entity} '{Key}' is incomplete (a provider failed); returned but not cached.", EntityKind, key);
            Telemetry.Requests.Add(1, new TagList { { "entity", EntityKind }, { "result", "partial" } });
            return fetched;
        }

        Persist(fetched);
        _logger.LogInformation("{Entity} '{Key}' cached", EntityKind, key);
        Telemetry.Requests.Add(1, new TagList { { "entity", EntityKind }, { "result", "external" } });
        return fetched;
    }


    private bool TryGetCached(TKey key, out TDto? dto)
    {
        TEntity? entity = GetCachedEntity(key);
        if (entity is null)
        {
            dto = null;
            return false;
        }

        _logger.LogInformation("{Entity} '{Key}' found in cache", EntityKind, key);
        Telemetry.Requests.Add(1, new TagList { { "entity", EntityKind }, { "result", "cache" } });
        if (IsNegativeHit(entity))
        {
            dto = null;
            return true;
        }

        dto = MapToDto(entity);
        dto.Origin = "Cache";
        return true;
    }
}
