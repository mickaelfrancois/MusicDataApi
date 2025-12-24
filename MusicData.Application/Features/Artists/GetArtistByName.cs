using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Artists;

public interface IGetArtistByName
{
    Task<ArtistDto?> HandleAsync(string name, CancellationToken cancellationToken = default);
}

public sealed class GetArtistByName(IArtistRepository artistRepository,
    IMusicAggregator musicAggregator,
    ILogger<GetArtistByName> logger) : IGetArtistByName
{
    public async Task<ArtistDto?> HandleAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        ArtistEntity? artistEntity = artistRepository.GetByName(name);
        if (artistEntity is not null)
        {
            logger.LogInformation("Artist '{ArtistName}' found in cache", name);
            ArtistDto dto = artistEntity.ToDto();
            dto.Origin = "Cache";
            return dto;
        }

        ArtistDto? artist = await musicAggregator.GetArtistAsync(name, cancellationToken);
        if (artist is null)
        {
            logger.LogInformation("Artist '{Name}' not found in any music service.", name);
            artist = new ArtistDto { Name = name, Origin = "NotFound" };
            artistRepository.Add(artist!.ToEntity());
            return null;
        }

        artistRepository.Add(artist!.ToEntity());
        logger.LogInformation("Artist '{ArtistName}' cached", name);

        return artist;
    }
}
