using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;
using MusicData.Shared;

namespace MusicData.Infrastructure.Repositories;

internal sealed class AlbumRepository(ILiteDatabase database)
    : LiteDbRepository<AlbumEntity>(database, CollectionName), IAlbumRepository
{
    public const string CollectionName = "albums";


    public static void EnsureIndexes(ILiteCollection<AlbumEntity> collection)
    {
        // Lookups compare LOWER(field), which a plain index on the field cannot
        // serve (full scan): index the same expressions the queries use, and
        // drop the plain indexes older databases still carry.
        collection.DropIndex("Name");
        collection.DropIndex("Artist");
        collection.DropIndex("MusicBrainzArtistID");
        collection.EnsureIndex("idx_name_lower", "LOWER($.Name)");
        collection.EnsureIndex("idx_mbid_lower", "LOWER($.MusicBrainzID)");
        collection.EnsureIndex(x => x.MusicBrainzID, unique: true);
        collection.EnsureIndex(AliasIndexName, "$.Aliases[*]");
    }

    internal const string AliasIndexName = "idx_aliases";
    internal const string ByNameQuery = "LOWER($.Name) = @0 AND LOWER($.MusicBrainzArtistID) = @1";
    internal const string ByAliasQuery = "$.Aliases[*] ANY = @0 AND LOWER($.MusicBrainzArtistID) = @1";


    protected override void MergeExisting(AlbumEntity incoming, AlbumEntity existing) =>
        incoming.Aliases = NameAliases.Merge(existing.Aliases, incoming.Aliases);


    protected override AlbumEntity? FindExisting(AlbumEntity incoming) =>
        !string.IsNullOrWhiteSpace(incoming.MusicBrainzID)
            ? FindByLowerField(nameof(AlbumEntity.MusicBrainzID), incoming.MusicBrainzID)
            : Collection.FindOne(
                "LOWER($.Name) = @0 AND LOWER($.Artist) = @1",
                (incoming.Name ?? string.Empty).ToLowerInvariant(),
                (incoming.Artist ?? string.Empty).ToLowerInvariant());


    public AlbumEntity? GetByMusicBrainzID(string musicBrainzID) =>
        FreshOrNull(FindByLowerField(nameof(AlbumEntity.MusicBrainzID), musicBrainzID));

    public AlbumEntity? GetByName(string albumName, string artistMusicBrainzId)
    {
        string loweredName = (albumName ?? string.Empty).ToLowerInvariant();
        string loweredArtistId = (artistMusicBrainzId ?? string.Empty).ToLowerInvariant();

        return FirstFreshOrNull(ByNameQuery, loweredName, loweredArtistId)
            ?? FirstFreshOrNull(ByAliasQuery, NameAliases.Normalize(albumName), loweredArtistId);
    }
}
