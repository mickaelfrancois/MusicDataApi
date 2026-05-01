using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;
using MusicData.Shared.Telemetry;

namespace MusicData.Application.Features.Albums;

public interface IGetAlbumByName
{
    Task<AlbumDto?> HandleAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken = default);
}

public sealed class GetAlbumByName(IAlbumRepository albumRepository,
    IMusicAggregator musicAggregator,
    IKeyedLocker keyedLocker,
    ILogger<GetAlbumByName> logger) : IGetAlbumByName
{
    public async Task<AlbumDto?> HandleAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(albumName) || string.IsNullOrWhiteSpace(artistMusicBrainzId))
            return null;

        if (TryGetCached(albumName, artistMusicBrainzId, out AlbumDto? cached))
            return cached;

        string lockKey = $"album:byname:{artistMusicBrainzId.ToLowerInvariant()}:{albumName.ToLowerInvariant()}";
        using IDisposable _ = await keyedLocker.LockAsync(lockKey, cancellationToken);

        if (TryGetCached(albumName, artistMusicBrainzId, out cached))
            return cached;

        AlbumDto? album = await musicAggregator.GetAlbumByNameAsync(albumName, artistMusicBrainzId, cancellationToken);
        if (album is null)
        {
            logger.LogInformation("Album '{Name}' not found in any music service.", albumName);
            Telemetry.Requests.Add(1, new TagList { { "entity", "album" }, { "result", "not_found" } });
            return null;
        }

        albumRepository.Add(album!.ToEntity());
        logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' cached", albumName, album.Artist);
        Telemetry.Requests.Add(1, new TagList { { "entity", "album" }, { "result", "external" } });

        return album;
    }


    private bool TryGetCached(string albumName, string artistMusicBrainzId, out AlbumDto? dto)
    {
        AlbumEntity? albumEntity = albumRepository.GetByName(albumName, artistMusicBrainzId);
        if (albumEntity is null)
        {
            dto = null;
            return false;
        }

        logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' was found in cache", albumEntity.Name, albumEntity.Artist);
        Telemetry.Requests.Add(1, new TagList { { "entity", "album" }, { "result", "cache" } });
        dto = albumEntity.ToDto();
        dto.Origin = "Cache";
        return true;
    }
}
