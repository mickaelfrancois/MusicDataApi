using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;
using MusicData.Shared;

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
        collection.EnsureIndex(AliasIndexName, "$.Aliases[*]");
    }

    internal const string AliasIndexName = "idx_aliases";
    internal const string ByNameQuery = "LOWER($.Name) = @0";
    internal const string ByAliasQuery = "$.Aliases[*] ANY = @0";


    protected override void MergeExisting(ArtistEntity incoming, ArtistEntity existing) =>
        incoming.Aliases = NameAliases.Merge(existing.Aliases, incoming.Aliases);


    protected override ArtistEntity? FindExisting(ArtistEntity incoming) =>
        !string.IsNullOrWhiteSpace(incoming.MusicBrainzID)
            ? FindByLowerField(nameof(ArtistEntity.MusicBrainzID), incoming.MusicBrainzID)
            : FindByLowerField(nameof(ArtistEntity.Name), incoming.Name);


    public ArtistEntity? GetByMusicBrainzID(string musicBrainzID) =>
        FreshOrNull(FindByLowerField(nameof(ArtistEntity.MusicBrainzID), musicBrainzID));

    public ArtistEntity? GetByName(string name)
    {
        string lowered = (name ?? string.Empty).ToLowerInvariant();

        return FirstFreshOrNull(ByNameQuery, lowered)
            ?? FirstFreshOrNull(ByAliasQuery, NameAliases.Normalize(name));
    }
}
