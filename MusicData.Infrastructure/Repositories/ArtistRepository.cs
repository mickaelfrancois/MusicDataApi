using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class ArtistRepository : IArtistRepository
{
    private readonly ILiteCollection<ArtistEntity> _collection;
    private const string CollectionName = "artists";

    // Bumped to 2 when P0-4 added Wikipedia, TikTok, Threads, SongKick, SoundCloud,
    // Imdb, Fanart4Url, Fanart5Url. Pre-fix rows (Version = 1) are treated as stale
    // so the handler falls through to the aggregator and re-populates them.
    private const int SchemaVersion = 2;

    public ArtistRepository(ILiteDatabase database)
    {
        _collection = database.GetCollection<ArtistEntity>(CollectionName);
        _collection.EnsureIndex(x => x.Name, unique: false);
        _collection.EnsureIndex(x => x.MusicBrainzID, unique: true);
    }


    public void Add(ArtistEntity artist)
    {
        ArtistEntity? existing = !string.IsNullOrWhiteSpace(artist.MusicBrainzID)
            ? FindByMusicBrainzID(artist.MusicBrainzID)
            : FindByName(artist.Name);

        artist.UpdateDateTime = DateTime.UtcNow;
        artist.Version = SchemaVersion;

        if (existing is not null)
            _collection.Update(existing.Id, artist);
        else
            _collection.Insert(artist);
    }


    public ArtistEntity? GetByMusicBrainzID(string musicBrainzID) => FreshOrNull(FindByMusicBrainzID(musicBrainzID));


    public ArtistEntity? GetByName(string name) => FreshOrNull(FindByName(name));


    private static ArtistEntity? FreshOrNull(ArtistEntity? entity) =>
        entity is null || entity.Version < SchemaVersion ? null : entity;


    private ArtistEntity? FindByMusicBrainzID(string musicBrainzID) =>
        _collection.FindOne("LOWER($.MusicBrainzID) = @0", (musicBrainzID ?? string.Empty).ToLowerInvariant());


    private ArtistEntity? FindByName(string name) =>
        _collection.FindOne("LOWER($.Name) = @0", (name ?? string.Empty).ToLowerInvariant());
}
