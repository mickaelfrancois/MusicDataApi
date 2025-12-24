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
        LyricsEntity? existing = null;

        existing ??= _collection.FindOne(c => c.Title.Equals(lyrics.Title, StringComparison.InvariantCultureIgnoreCase)
                                            && c.ArtistName.Equals(lyrics.ArtistName, StringComparison.InvariantCultureIgnoreCase));

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


    public void Delete(int id)
    {
        _collection.Delete(id);
    }


    public LyricsEntity? Get(string title, string artistName)
    {
        return _collection.FindOne(c => c.Title.Equals(title, StringComparison.InvariantCultureIgnoreCase)
                                  && c.ArtistName.Equals(artistName, StringComparison.InvariantCultureIgnoreCase));
    }


    public void Update(LyricsEntity lyrics)
    {
        _collection.Update(lyrics);
    }
}