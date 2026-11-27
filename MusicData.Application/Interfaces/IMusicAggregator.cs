using MusicData.Application.DTOs;

namespace MusicData.Application.Interfaces;

public interface IMusicAggregator
{
    Task<ArtistDto?> GetArtistByNameAsync(string name, CancellationToken cancellationToken);

    Task<ArtistDto?> GetArtistByMusicBrainzIdAsync(string musicBrainzId, CancellationToken cancellationToken);

    Task<AlbumDto?> GetAlbumByNameAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken);

    /// <summary>
    /// The artist MBID and release-group id are derived from the MusicBrainz
    /// release itself, never taken from the caller.
    /// </summary>
    Task<AlbumDto?> GetAlbumByMusicBrainzIdAsync(string albumMusicBrainzId, CancellationToken cancellationToken);
}
