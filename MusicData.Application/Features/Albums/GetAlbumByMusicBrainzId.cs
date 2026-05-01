using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;
using MusicData.Shared.Telemetry;

namespace MusicData.Application.Features.Albums;

public interface IGetAlbumByMusicBrainzId
{
    Task<AlbumDto?> HandleAsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken = default);
}

public sealed class GetAlbumByMusicBrainzId(IAlbumRepository albumRepository,
    IMusicAggregator musicAggregator,
    IKeyedLocker keyedLocker,
    ILogger<GetAlbumByName> logger) : IGetAlbumByMusicBrainzId
{
    public async Task<AlbumDto?> HandleAsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(albumMusicBrainzId) || string.IsNullOrWhiteSpace(artistMusicBrainzId))
            return null;

        if (TryGetCached(albumMusicBrainzId, out AlbumDto? cached))
            return cached;

        using IDisposable _ = await keyedLocker.LockAsync($"album:bymbid:{albumMusicBrainzId.ToLowerInvariant()}", cancellationToken);

        if (TryGetCached(albumMusicBrainzId, out cached))
            return cached;

        AlbumDto? album = await musicAggregator.GetAlbumByMusicBrainzIdsync(albumMusicBrainzId, artistMusicBrainzId, cancellationToken);
        if (album is null)
        {
            logger.LogInformation("Album '{Name}' not found in any music service.", albumMusicBrainzId);
            Telemetry.Requests.Add(1, new TagList { { "entity", "album" }, { "result", "not_found" } });
            return null;
        }

        albumRepository.Add(album!.ToEntity());
        logger.LogInformation("Album '{AlbumName}' of '{ArtistName}' cached", albumMusicBrainzId, album.Artist);
        Telemetry.Requests.Add(1, new TagList { { "entity", "album" }, { "result", "external" } });

        return album;
    }


    private bool TryGetCached(string albumMusicBrainzId, out AlbumDto? dto)
    {
        AlbumEntity? albumEntity = albumRepository.GetByMusicBrainzID(albumMusicBrainzId);
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
