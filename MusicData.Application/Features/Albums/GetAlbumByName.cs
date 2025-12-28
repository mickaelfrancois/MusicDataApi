using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Albums;

public interface IGetAlbumByName
{
    Task<AlbumDto?> HandleAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken = default);
}

public sealed class GetAlbumByName(IAlbumRepository albumRepository, IMusicAggregator musicAggregator, ILogger<GetAlbumByName> logger) : IGetAlbumByName
{
    public async Task<AlbumDto?> HandleAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(albumName) || string.IsNullOrWhiteSpace(artistMusicBrainzId))
            return null;


        AlbumEntity? albumEntity = albumRepository.GetByName(albumName, artistMusicBrainzId);
        if (albumEntity is not null)
        {
            logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' was found in cache", albumEntity.Name, albumEntity.Artist);
            AlbumDto dto = albumEntity.ToDto();
            dto.Origin = "Cache";
            return dto;
        }

        AlbumDto? album = await musicAggregator.GetAlbumByNameAsync(albumName, artistMusicBrainzId, cancellationToken);
        if (album is null)
        {
            logger.LogInformation("Album '{Name}' not found in any music service.", albumName);
            return null;
        }

        albumRepository.Add(album!.ToEntity());
        logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' cached", albumName, album.Artist);

        return album;
    }
}
