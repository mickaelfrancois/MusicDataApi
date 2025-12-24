using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class AlbumRepository : IAlbumRepository
{
    private readonly ILiteCollection<AlbumEntity> _collection;
    private const string CollectionName = "albums";

    public AlbumRepository(ILiteDatabase database)
    {
        _collection = database.GetCollection<AlbumEntity>(CollectionName);
        _collection.EnsureIndex(x => x.Name, unique: false);
        _collection.EnsureIndex(x => x.Artist, unique: false);
        _collection.EnsureIndex(x => x.MusicBrainzID, unique: false);
    }


    public void Add(AlbumEntity album)
    {
        AlbumEntity? existing = null;

        if (!string.IsNullOrWhiteSpace(album.MusicBrainzID))
            existing = _collection.FindOne(c => c.MusicBrainzID.Equals(album.MusicBrainzID, StringComparison.InvariantCultureIgnoreCase));

        existing ??= _collection.FindOne(c => c.Name.Equals(album.Name, StringComparison.InvariantCultureIgnoreCase));

        if (existing is not null)
        {
            album.UpdateDateTime = DateTime.UtcNow;
            album.Version = 1;
            _collection.Update(existing.Id, album);
        }
        else
        {
            album.UpdateDateTime = DateTime.UtcNow;
            album.Version = 1;
            _collection.Insert(album);
        }
    }


    public void Delete(int id)
    {
        _collection.Delete(id);
    }


    public AlbumEntity? GetByMusicBrainzID(string musicBrainzID)
    {
        return _collection.FindOne(c => c.MusicBrainzID.Equals(musicBrainzID, StringComparison.InvariantCultureIgnoreCase));
    }


    public AlbumEntity? GetByName(string albumName, string artistName)
    {
        return _collection.FindOne(c => c.Name.Equals(albumName, StringComparison.InvariantCultureIgnoreCase)
                                    && c.Artist.Equals(artistName, StringComparison.InvariantCultureIgnoreCase));
    }


    public void Update(AlbumEntity album)
    {
        _collection.Update(album);
    }
}
