using MusicData.Domain.Entities;

namespace MusicData.Application.Interfaces;

public interface IArtistRepository
{
    ArtistEntity? GetByMusicBrainzID(string musicBrainzID);

    ArtistEntity? GetByName(string name);

    void Add(ArtistEntity artist);
}
