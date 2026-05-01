using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;
using MusicData.Shared.Telemetry;

namespace MusicData.Application.Features.Artists;

public interface IGetArtistByName
{
    Task<ArtistDto?> HandleAsync(string artistName, CancellationToken cancellationToken = default);
}

public sealed class GetArtistByName(IArtistRepository artistRepository,
    IMusicAggregator musicAggregator,
    IKeyedLocker keyedLocker,
    ILogger<GetArtistByName> logger) : IGetArtistByName
{
    public async Task<ArtistDto?> HandleAsync(string artistName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artistName))
            return null;

        if (TryGetCached(artistName, out ArtistDto? cached))
            return cached;

        using IDisposable _ = await keyedLocker.LockAsync($"artist:byname:{artistName.ToLowerInvariant()}", cancellationToken);

        if (TryGetCached(artistName, out cached))
            return cached;

        ArtistDto? artist = await musicAggregator.GetArtistByNameAsync(artistName, cancellationToken);
        if (artist is null)
        {
            logger.LogInformation("Artist '{Name}' not found in any music service.", artistName);
            Telemetry.Requests.Add(1, new TagList { { "entity", "artist" }, { "result", "not_found" } });
            return null;
        }

        artistRepository.Add(artist!.ToEntity());
        logger.LogInformation("Artist '{ArtistName}' cached", artistName);
        Telemetry.Requests.Add(1, new TagList { { "entity", "artist" }, { "result", "external" } });

        return artist;
    }


    private bool TryGetCached(string artistName, out ArtistDto? dto)
    {
        ArtistEntity? artistEntity = artistRepository.GetByName(artistName);
        if (artistEntity is null)
        {
            dto = null;
            return false;
        }

        logger.LogInformation("Artist '{ArtistName}' found in cache", artistName);
        Telemetry.Requests.Add(1, new TagList { { "entity", "artist" }, { "result", "cache" } });
        dto = artistEntity.ToDto();
        dto.Origin = "Cache";
        return true;
    }
}
