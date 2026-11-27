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
        // Lookups compare LOWER(field), which a plain index on the field cannot
        // serve (full scan): index the same expressions the queries use, and
        // drop the plain indexes older databases still carry.
        collection.DropIndex("Title");
        collection.DropIndex("ArtistName");
        collection.EnsureIndex("idx_title_lower", "LOWER($.Title)");
    }

    internal const string ByTitleAndArtistQuery = "LOWER($.Title) = @0 AND LOWER($.ArtistName) = @1";


    protected override LyricsEntity? FindExisting(LyricsEntity incoming) =>
        FindByTitleAndArtist(incoming.Title, incoming.ArtistName);


    public LyricsEntity? Get(string title, string artistName) =>
        FreshOrNull(FindByTitleAndArtist(title, artistName));


    private LyricsEntity? FindByTitleAndArtist(string title, string artistName) =>
        Collection.FindOne(
            ByTitleAndArtistQuery,
            (title ?? string.Empty).ToLowerInvariant(),
            (artistName ?? string.Empty).ToLowerInvariant());
}
