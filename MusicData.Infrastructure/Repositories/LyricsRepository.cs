using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class LyricsRepository : ILyricsRepository
{
    private readonly ILiteCollection<LyricsEntity> _collection;
    private const string CollectionName = "lyrics";

    // Bumped to 2 when P2-2 added AlbumName / Duration to LyricsEntity.
    // Pre-fix rows (Version = 1) are treated as stale so the handler falls
    // through to the aggregator and re-populates them with the new fields.
    private const int SchemaVersion = 2;

    public LyricsRepository(ILiteDatabase database)
    {
        _collection = database.GetCollection<LyricsEntity>(CollectionName);
        _collection.EnsureIndex(x => x.Title, unique: false);
        _collection.EnsureIndex(x => x.ArtistName, unique: false);
    }


    public void Add(LyricsEntity lyrics)
    {
        LyricsEntity? existing = FindByTitleAndArtist(lyrics.Title, lyrics.ArtistName);

        lyrics.UpdateDateTime = DateTime.UtcNow;
        lyrics.Version = SchemaVersion;

        if (existing is not null)
            _collection.Update(existing.Id, lyrics);
        else
            _collection.Insert(lyrics);
    }


    public LyricsEntity? Get(string title, string artistName) => FreshOrNull(FindByTitleAndArtist(title, artistName));


    private static LyricsEntity? FreshOrNull(LyricsEntity? entity) =>
        entity is null || entity.Version < SchemaVersion ? null : entity;


    private LyricsEntity? FindByTitleAndArtist(string title, string artistName) =>
        _collection.FindOne(
            "LOWER($.Title) = @0 AND LOWER($.ArtistName) = @1",
            (title ?? string.Empty).ToLowerInvariant(),
            (artistName ?? string.Empty).ToLowerInvariant());
}
