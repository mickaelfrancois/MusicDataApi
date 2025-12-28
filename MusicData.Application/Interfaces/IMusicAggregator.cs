using MusicData.Application.DTOs;

namespace MusicData.Application.Interfaces;

public interface IMusicAggregator
{
    Task<ArtistDto?> GetArtistByNameAsync(string name, CancellationToken cancellationToken);

    Task<ArtistDto?> GetArtistByMusicBrainzIdAsync(string musicBrainzId, CancellationToken cancellationToken);

    Task<AlbumDto?> GetAlbumByNameAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken);

    Task<AlbumDto?> GetAlbumByMusicBrainzIdsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken);
}
