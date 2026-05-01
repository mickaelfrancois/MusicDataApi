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
        EnsureIndexes(Collection);
    }

    /// <summary>
    /// Bumped by subclasses whenever the entity shape changes; rows persisted
    /// at a lower version are surfaced as cache misses by <see cref="FreshOrNull"/>
    /// so the handler refetches and rewrites them with the current schema.
    /// </summary>
    protected virtual int SchemaVersion => 1;

    /// <summary>
    /// Subclasses register their indexes once per collection here.
    /// </summary>
    protected abstract void EnsureIndexes(ILiteCollection<TEntity> collection);

    /// <summary>
    /// Returns the row that <paramref name="incoming"/> should overwrite, or
    /// null when there is no existing row to merge into.
    /// </summary>
    protected abstract TEntity? FindExisting(TEntity incoming);


    public void Add(TEntity entity)
    {
        TEntity? existing = FindExisting(entity);

        entity.UpdateDateTime = DateTime.UtcNow;
        entity.Version = SchemaVersion;

        if (existing is not null)
        {
            entity.Id = existing.Id;
            Collection.Update(entity);
        }
        else
        {
            Collection.Insert(entity);
        }
    }


    /// <summary>
    /// Treats rows persisted under an older schema as cache misses so the
    /// handler will refresh them.
    /// </summary>
    protected TEntity? FreshOrNull(TEntity? entity) =>
        entity is null || entity.Version < SchemaVersion ? null : entity;


    /// <summary>
    /// Case-insensitive single-field lookup using LiteDB's LOWER() BSON
    /// function. The field name is the BSON property name (which matches the
    /// .NET property name for these POCOs).
    /// </summary>
    protected TEntity? FindByLowerField(string field, string? value) =>
        Collection.FindOne($"LOWER($.{field}) = @0", (value ?? string.Empty).ToLowerInvariant());
}
