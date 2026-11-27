using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class LyricsRepository(ILiteDatabase database)
    : LiteDbRepository<LyricsEntity>(database, CollectionName), ILyricsRepository
{
    public const string CollectionName = "lyrics";

    // Bumped to 2 when P2-2 added AlbumName / Duration to LyricsEntity.
    // Bumped to 3 when NotFound was added: older negative markers carried no
    // flag and would otherwise be served as empty lyrics.
    protected override int SchemaVersion => 3;


    // Negative markers expire quickly: lyrics may be published later or a
    // provider may simply have been down when the marker was written.
    protected override TimeSpan TimeToLive(LyricsEntity entity) =>
        entity.NotFound ? TimeSpan.FromDays(1) : base.TimeToLive(entity);


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
