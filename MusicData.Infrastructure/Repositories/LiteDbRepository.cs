using LiteDB;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal abstract class LiteDbRepository<TEntity>
    where TEntity : class, IVersionedEntity
{
    protected ILiteCollection<TEntity> Collection { get; }

    protected LiteDbRepository(ILiteDatabase database, string collectionName)
    {
        Collection = database.GetCollection<TEntity>(collectionName);
    }

    /// <summary>
    /// Bumped by subclasses whenever the entity shape changes; rows persisted
    /// at a lower version are surfaced as cache misses by <see cref="FreshOrNull"/>
    /// so the handler refetches and rewrites them with the current schema.
    /// </summary>
    protected virtual int SchemaVersion => 1;

    /// <summary>
    /// How long a cached row stays valid. Expired rows are treated as cache
    /// misses (and overwritten in place by the next <see cref="Add"/>), so
    /// provider data never goes stale forever.
    /// </summary>
    protected virtual TimeSpan TimeToLive(TEntity entity) => TimeSpan.FromDays(30);

    /// <summary>
    /// Returns the row that <paramref name="incoming"/> should overwrite, or
    /// null when there is no existing row to merge into.
    /// </summary>
    protected abstract TEntity? FindExisting(TEntity incoming);


    public void Add(TEntity entity)
    {
        entity.UpdateDateTime = DateTime.UtcNow;
        entity.Version = SchemaVersion;

        if (!TryUpsert(entity))
        {
            // Lost a race: a concurrent request (different lock key, e.g. by-name
            // vs by-mbid) inserted the same unique key between our lookup and
            // insert. The row exists now, so update it instead of failing.
            if (!TryUpsert(entity))
                throw new InvalidOperationException($"Could not upsert {typeof(TEntity).Name} after a duplicate-key retry.");
        }
    }


    private bool TryUpsert(TEntity entity)
    {
        TEntity? existing = FindExisting(entity);

        if (existing is not null)
        {
            entity.Id = existing.Id;
            Collection.Update(entity);
            return true;
        }

        try
        {
            Collection.Insert(entity);
            return true;
        }
        catch (LiteException ex) when (ex.ErrorCode == LiteException.INDEX_DUPLICATE_KEY)
        {
            return false;
        }
    }


    /// <summary>
    /// Treats rows persisted under an older schema, or older than their
    /// <see cref="TimeToLive"/>, as cache misses so the handler refreshes them.
    /// </summary>
    protected TEntity? FreshOrNull(TEntity? entity) =>
        entity is null
        || entity.Version < SchemaVersion
        || DateTime.UtcNow - entity.UpdateDateTime > TimeToLive(entity)
            ? null
            : entity;


    /// <summary>
    /// Case-insensitive single-field lookup using LiteDB's LOWER() BSON
    /// function. The field name is the BSON property name (which matches the
    /// .NET property name for these POCOs).
    /// </summary>
    protected TEntity? FindByLowerField(string field, string? value) =>
        Collection.FindOne($"LOWER($.{field}) = @0", (value ?? string.Empty).ToLowerInvariant());
}
