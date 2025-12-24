using MusicData.Application.DTOs;

namespace MusicData.Application.Interfaces;

public interface IMusicService
{
    Task<ArtistDto?> GetArtistAsync(string musicBrainzId, CancellationToken cancellationToken);

    Task<AlbumDto?> GetAlbumAsync(string releaseMusicBrainzId, string? releaseGroupMusicBrainzId, CancellationToken cancellationToken);
}
