using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;
using MusicData.Shared.Telemetry;

namespace MusicData.Application.Features.Lyrics;

public interface IGetLyrics
{
    Task<LyricsDto?> HandleAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken = default);
}

public sealed class GetLyrics(ILyricsRepository lyricsRepository,
    ILyricsAggregator lyricsAggregator,
    IKeyedLocker keyedLocker,
    ILogger<GetLyrics> logger) : IGetLyrics
{
    public async Task<LyricsDto?> HandleAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrEmpty(artistName))
            return null;

        if (TryGetCached(title, artistName, out LyricsDto? cached))
            return cached;

        string lockKey = $"lyrics:{artistName.ToLowerInvariant()}:{title.ToLowerInvariant()}";
        using IDisposable _ = await keyedLocker.LockAsync(lockKey, cancellationToken);

        if (TryGetCached(title, artistName, out cached))
            return cached;

        LyricsDto? lyrics = await lyricsAggregator.GetLyricsAsync(title, artistName, albumName, duration, cancellationToken);
        if (lyrics is null)
        {
            logger.LogInformation("Lyrics '{Title}' not found in any music service.", title);
            Telemetry.Requests.Add(1, new TagList { { "entity", "lyrics" }, { "result", "not_found" } });
            lyrics = new LyricsDto { Title = title, ArtistName = artistName, Origin = "NotFound" };
            lyricsRepository.Add(lyrics!.ToEntity());
            return null;
        }

        lyricsRepository.Add(lyrics!.ToEntity());
        logger.LogInformation("Lyrics '{Title}' cached", title);
        Telemetry.Requests.Add(1, new TagList { { "entity", "lyrics" }, { "result", "external" } });

        return lyrics;
    }


    private bool TryGetCached(string title, string artistName, out LyricsDto? dto)
    {
        LyricsEntity? lyricsEntity = lyricsRepository.Get(title, artistName);
        if (lyricsEntity is null)
        {
            dto = null;
            return false;
        }

        logger.LogInformation("Lyrics '{Title}' found in cache", title);
        Telemetry.Requests.Add(1, new TagList { { "entity", "lyrics" }, { "result", "cache" } });
        dto = lyricsEntity.ToDto();
        dto.Origin = "Cache";
        return true;
    }
}
