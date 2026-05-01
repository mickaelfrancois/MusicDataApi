using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class ArtistRepository(ILiteDatabase database)
    : LiteDbRepository<ArtistEntity>(database, CollectionName), IArtistRepository
{
    public const string CollectionName = "artists";

    // Bumped to 2 when P0-4 added Wikipedia, TikTok, Threads, SongKick,
    // SoundCloud, Imdb, Fanart4Url and Fanart5Url. Pre-fix rows are stale.
    protected override int SchemaVersion => 2;


    public static void EnsureIndexes(ILiteCollection<ArtistEntity> collection)
    {
        collection.EnsureIndex(x => x.Name, unique: false);
        collection.EnsureIndex(x => x.MusicBrainzID, unique: true);
    }


    protected override ArtistEntity? FindExisting(ArtistEntity incoming) =>
        !string.IsNullOrWhiteSpace(incoming.MusicBrainzID)
            ? FindByLowerField(nameof(ArtistEntity.MusicBrainzID), incoming.MusicBrainzID)
            : FindByLowerField(nameof(ArtistEntity.Name), incoming.Name);


    public ArtistEntity? GetByMusicBrainzID(string musicBrainzID) =>
        FreshOrNull(FindByLowerField(nameof(ArtistEntity.MusicBrainzID), musicBrainzID));

    public ArtistEntity? GetByName(string name) =>
        FreshOrNull(FindByLowerField(nameof(ArtistEntity.Name), name));
}
