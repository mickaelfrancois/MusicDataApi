using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class LyricsRepository : ILyricsRepository
{
    private readonly ILiteCollection<LyricsEntity> _collection;
    private const string CollectionName = "lyrics";

    public LyricsRepository(ILiteDatabase database)
    {
        _collection = database.GetCollection<LyricsEntity>(CollectionName);
        _collection.EnsureIndex(x => x.Title, unique: false);
        _collection.EnsureIndex(x => x.ArtistName, unique: false);
    }


    public void Add(LyricsEntity lyrics)
    {
        LyricsEntity? existing = FindByTitleAndArtist(lyrics.Title, lyrics.ArtistName);

        if (existing is not null)
        {
            lyrics.UpdateDateTime = DateTime.UtcNow;
            lyrics.Version = 1;
            _collection.Update(existing.Id, lyrics);
        }
        else
        {
            lyrics.UpdateDateTime = DateTime.UtcNow;
            lyrics.Version = 1;
            _collection.Insert(lyrics);
        }
    }


    public LyricsEntity? Get(string title, string artistName) => FindByTitleAndArtist(title, artistName);


    private LyricsEntity? FindByTitleAndArtist(string title, string artistName) =>
        _collection.FindOne(
            "LOWER($.Title) = @0 AND LOWER($.ArtistName) = @1",
            (title ?? string.Empty).ToLowerInvariant(),
            (artistName ?? string.Empty).ToLowerInvariant());
}
