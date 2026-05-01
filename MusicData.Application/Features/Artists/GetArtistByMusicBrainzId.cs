using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;
using MusicData.Shared.Telemetry;

namespace MusicData.Application.Features.Artists;

public interface IGetArtistByMusicBrainzId
{
    Task<ArtistDto?> HandleAsync(string musicBrainzId, CancellationToken cancellationToken = default);
}

public sealed class GetArtistByMusicBrainzId(IArtistRepository artistRepository,
    IMusicAggregator musicAggregator,
    IKeyedLocker keyedLocker,
    ILogger<GetArtistByName> logger) : IGetArtistByMusicBrainzId
{
    public async Task<ArtistDto?> HandleAsync(string musicBrainzId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        if (TryGetCached(musicBrainzId, out ArtistDto? cached))
            return cached;

        using IDisposable _ = await keyedLocker.LockAsync($"artist:bymbid:{musicBrainzId.ToLowerInvariant()}", cancellationToken);

        if (TryGetCached(musicBrainzId, out cached))
            return cached;

        ArtistDto? artist = await musicAggregator.GetArtistByMusicBrainzIdAsync(musicBrainzId, cancellationToken);
        if (artist is null)
        {
            logger.LogInformation("Artist '{MusicBrainzId}' not found in any music service.", musicBrainzId);
            Telemetry.Requests.Add(1, new TagList { { "entity", "artist" }, { "result", "not_found" } });
            return null;
        }

        artistRepository.Add(artist!.ToEntity());
        logger.LogInformation("Artist '{ArtistName}' cached", artist.Name);
        Telemetry.Requests.Add(1, new TagList { { "entity", "artist" }, { "result", "external" } });

        return artist;
    }


    private bool TryGetCached(string musicBrainzId, out ArtistDto? dto)
    {
        ArtistEntity? artistEntity = artistRepository.GetByMusicBrainzID(musicBrainzId);
        if (artistEntity is null)
        {
            dto = null;
            return false;
        }

        logger.LogInformation("Artist '{ArtistName}' found in cache", artistEntity.Name);
        Telemetry.Requests.Add(1, new TagList { { "entity", "artist" }, { "result", "cache" } });
        dto = artistEntity.ToDto();
        dto.Origin = "Cache";
        return true;
    }
}
