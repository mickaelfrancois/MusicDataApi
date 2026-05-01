using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class LyricsRepository(ILiteDatabase database)
    : LiteDbRepository<LyricsEntity>(database, CollectionName), ILyricsRepository
{
    public const string CollectionName = "lyrics";

    // Bumped to 2 when P2-2 added AlbumName / Duration to LyricsEntity.
    protected override int SchemaVersion => 2;


    public static void EnsureIndexes(ILiteCollection<LyricsEntity> collection)
    {
        collection.EnsureIndex(x => x.Title, unique: false);
        collection.EnsureIndex(x => x.ArtistName, unique: false);
    }


    protected override LyricsEntity? FindExisting(LyricsEntity incoming) =>
        FindByTitleAndArtist(incoming.Title, incoming.ArtistName);


    public LyricsEntity? Get(string title, string artistName) =>
        FreshOrNull(FindByTitleAndArtist(title, artistName));


    private LyricsEntity? FindByTitleAndArtist(string title, string artistName) =>
        Collection.FindOne(
            "LOWER($.Title) = @0 AND LOWER($.ArtistName) = @1",
            (title ?? string.Empty).ToLowerInvariant(),
            (artistName ?? string.Empty).ToLowerInvariant());
}
