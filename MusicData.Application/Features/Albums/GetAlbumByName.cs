using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Albums;

public interface IGetAlbumByName
{
    Task<AlbumDto?> HandleAsync(string albumName, string artistName, CancellationToken cancellationToken = default);
}

public sealed class GetAlbumByName(IAlbumRepository albumRepository,
    IMusicAggregator musicAggregator,
    ILogger<GetAlbumByName> logger) : IGetAlbumByName
{
    public async Task<AlbumDto?> HandleAsync(string albumName, string artistName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(albumName))
            return null;

        AlbumEntity? albumEntity = albumRepository.GetByName(albumName, artistName);
        if (albumEntity is not null)
        {
            logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' was found in cache", albumName, artistName);
            AlbumDto dto = albumEntity.ToDto();
            dto.Origin = "Cache";
            return dto;
        }

        AlbumDto? album = await musicAggregator.GetAlbumAsync(albumName, artistName, cancellationToken);
        if (album is null)
        {
            logger.LogInformation("Album '{Name}' not found in any music service.", albumName);
            album = new AlbumDto { Name = albumName, Artist = artistName, Origin = "NotFound" };
            albumRepository.Add(album!.ToEntity());

            return null;
        }

        albumRepository.Add(album!.ToEntity());
        logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' cached", albumName, artistName);

        return album;
    }
}
