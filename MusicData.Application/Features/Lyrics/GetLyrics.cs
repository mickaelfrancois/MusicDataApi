using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Lyrics;

public interface IGetLyrics
{
    Task<LyricsDto?> HandleAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken = default);
}

public sealed class GetLyrics(ILyricsRepository lyricsRepository,
    ILyricsAggregator lyricsAggregator,
    ILogger<GetLyrics> logger) : IGetLyrics
{
    public async Task<LyricsDto?> HandleAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrEmpty(artistName))
            return null;

        LyricsEntity? lyricsEntity = lyricsRepository.Get(title, artistName);
        if (lyricsEntity is not null)
        {
            logger.LogInformation("Lyrics '{Title}' found in cache", title);
            LyricsDto dto = lyricsEntity.ToDto();
            dto.Origin = "Cache";
            return dto;
        }

        LyricsDto? lyrics = await lyricsAggregator.GetLyricsAsync(title, artistName, albumName, duration, cancellationToken);
        if (lyrics is null)
        {
            logger.LogInformation("Lyrics '{Title}' not found in any music service.", title);
            lyrics = new LyricsDto { Title = title, ArtistName = artistName, Origin = "NotFound" };
            lyricsRepository.Add(lyrics!.ToEntity());
            return null;
        }

        lyricsRepository.Add(lyrics!.ToEntity());
        logger.LogInformation("Lyrics '{Title}' cached", title);

        return lyrics;
    }
}
