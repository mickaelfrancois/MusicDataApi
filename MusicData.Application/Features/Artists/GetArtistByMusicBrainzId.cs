using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Artists;

public interface IGetArtistByMusicBrainzId
{
    Task<ArtistDto?> HandleAsync(string musicBrainzId, CancellationToken cancellationToken = default);
}

public sealed class GetArtistByMusicBrainzId(IArtistRepository artistRepository, IMusicAggregator musicAggregator, ILogger<GetArtistByName> logger) : IGetArtistByMusicBrainzId
{
    public async Task<ArtistDto?> HandleAsync(string musicBrainzId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        ArtistEntity? artistEntity = artistRepository.GetByMusicBrainzID(musicBrainzId);

        if (artistEntity is not null)
        {
            logger.LogInformation("Artist '{ArtistName}' found in cache", artistEntity.Name);
            ArtistDto dto = artistEntity.ToDto();
            dto.Origin = "Cache";
            return dto;
        }

        ArtistDto? artist = await musicAggregator.GetArtistByMusicBrainzIdAsync(musicBrainzId, cancellationToken);
        if (artist is null)
        {
            logger.LogInformation("Artist '{MusicBrainzId}' not found in any music service.", musicBrainzId);
            return null;
        }

        artistRepository.Add(artist!.ToEntity());
        logger.LogInformation("Artist '{ArtistName}' cached", artist.Name);

        return artist;
    }
}
