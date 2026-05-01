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
        _collection.EnsureIndex(x => x.MusicBrainzArtistID, unique: false);
        _collection.EnsureIndex(x => x.MusicBrainzID, unique: true);
    }


    public void Add(AlbumEntity album)
    {
        AlbumEntity? existing = !string.IsNullOrWhiteSpace(album.MusicBrainzID)
            ? FindByMusicBrainzID(album.MusicBrainzID)
            : _collection.FindOne(
                "LOWER($.Name) = @0 AND LOWER($.Artist) = @1",
                (album.Name ?? string.Empty).ToLowerInvariant(),
                (album.Artist ?? string.Empty).ToLowerInvariant());

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


    public AlbumEntity? GetByMusicBrainzID(string musicBrainzID) => FindByMusicBrainzID(musicBrainzID);


    public AlbumEntity? GetByName(string albumName, string artistName) =>
        _collection.FindOne(
            "LOWER($.Name) = @0 AND LOWER($.Artist) = @1",
            (albumName ?? string.Empty).ToLowerInvariant(),
            (artistName ?? string.Empty).ToLowerInvariant());


    public void Update(AlbumEntity album)
    {
        _collection.Update(album);
    }


    private AlbumEntity? FindByMusicBrainzID(string musicBrainzID) =>
        _collection.FindOne("LOWER($.MusicBrainzID) = @0", (musicBrainzID ?? string.Empty).ToLowerInvariant());
}
