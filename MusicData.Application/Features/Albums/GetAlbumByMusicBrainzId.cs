using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Albums;

public interface IGetAlbumByMusicBrainzId
{
    Task<AlbumDto?> HandleAsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken = default);
}

public sealed class GetAlbumByMusicBrainzId(IAlbumRepository albumRepository, IMusicAggregator musicAggregator, ILogger<GetAlbumByName> logger) : IGetAlbumByMusicBrainzId
{
    public async Task<AlbumDto?> HandleAsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(albumMusicBrainzId) || string.IsNullOrWhiteSpace(artistMusicBrainzId))
            return null;

        AlbumEntity? albumEntity = albumRepository.GetByMusicBrainzID(albumMusicBrainzId);
        if (albumEntity is not null)
        {
            logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' was found in cache", albumEntity.Name, albumEntity.Artist);
            AlbumDto dto = albumEntity.ToDto();
            dto.Origin = "Cache";
            return dto;
        }

        AlbumDto? album = await musicAggregator.GetAlbumByMusicBrainzIdsync(albumMusicBrainzId, artistMusicBrainzId, cancellationToken);
        if (album is null)
        {
            logger.LogInformation("Album '{Name}' not found in any music service.", albumMusicBrainzId);
            return null;
        }

        albumRepository.Add(album!.ToEntity());
        logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' cached", albumMusicBrainzId, album.Artist);

        return album;
    }
}
