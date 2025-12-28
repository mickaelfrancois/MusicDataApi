using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Artists;

public interface IGetArtistByName
{
    Task<ArtistDto?> HandleAsync(string artistName, CancellationToken cancellationToken = default);
}

public sealed class GetArtistByName(IArtistRepository artistRepository,
    IMusicAggregator musicAggregator,
    ILogger<GetArtistByName> logger) : IGetArtistByName
{
    public async Task<ArtistDto?> HandleAsync(string artistName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artistName))
            return null;

        ArtistEntity? artistEntity = artistRepository.GetByName(artistName!);

        if (artistEntity is not null)
        {
            logger.LogInformation("Artist '{ArtistName}' found in cache", artistName);
            ArtistDto dto = artistEntity.ToDto();
            dto.Origin = "Cache";
            return dto;
        }

        ArtistDto? artist = await musicAggregator.GetArtistByNameAsync(artistName, cancellationToken);
        if (artist is null)
        {
            logger.LogInformation("Artist '{Name}' not found in any music service.", artistName);
            return null;
        }

        artistRepository.Add(artist!.ToEntity());
        logger.LogInformation("Artist '{ArtistName}' cached", artistName);

        return artist;
    }
}
