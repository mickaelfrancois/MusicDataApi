using MusicData.Application.DTOs;

namespace MusicData.Application.Interfaces;

public interface IMusicAggregator
{
    Task<ArtistDto?> GetArtistAsync(string name, CancellationToken cancellationToken);

    Task<AlbumDto?> GetAlbumAsync(string albumName, string artistName, CancellationToken cancellationToken);
}
