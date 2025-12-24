using MusicData.Domain.Entities;

namespace MusicData.Application.Interfaces;

public interface IAlbumRepository
{
    AlbumEntity? GetByMusicBrainzID(string musicBrainzID);

    AlbumEntity? GetByName(string albumName, string artistName);

    void Add(AlbumEntity album);

    void Update(AlbumEntity album);

    void Delete(int id);
}
