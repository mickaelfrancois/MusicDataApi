using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class AlbumRepository(ILiteDatabase database)
    : LiteDbRepository<AlbumEntity>(database, CollectionName), IAlbumRepository
{
    public const string CollectionName = "albums";


    public static void EnsureIndexes(ILiteCollection<AlbumEntity> collection)
    {
        collection.EnsureIndex(x => x.Name, unique: false);
        collection.EnsureIndex(x => x.Artist, unique: false);
        collection.EnsureIndex(x => x.MusicBrainzArtistID, unique: false);
        collection.EnsureIndex(x => x.MusicBrainzID, unique: true);
    }


    protected override AlbumEntity? FindExisting(AlbumEntity incoming) =>
        !string.IsNullOrWhiteSpace(incoming.MusicBrainzID)
            ? FindByLowerField(nameof(AlbumEntity.MusicBrainzID), incoming.MusicBrainzID)
            : Collection.FindOne(
                "LOWER($.Name) = @0 AND LOWER($.Artist) = @1",
                (incoming.Name ?? string.Empty).ToLowerInvariant(),
                (incoming.Artist ?? string.Empty).ToLowerInvariant());


    public AlbumEntity? GetByMusicBrainzID(string musicBrainzID) =>
        FreshOrNull(FindByLowerField(nameof(AlbumEntity.MusicBrainzID), musicBrainzID));

    public AlbumEntity? GetByName(string albumName, string artistName) =>
        FreshOrNull(Collection.FindOne(
            "LOWER($.Name) = @0 AND LOWER($.Artist) = @1",
            (albumName ?? string.Empty).ToLowerInvariant(),
            (artistName ?? string.Empty).ToLowerInvariant()));
}
