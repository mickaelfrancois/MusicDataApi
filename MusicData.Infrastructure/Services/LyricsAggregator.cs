using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.RateLimiting;

namespace MusicData.Infrastructure.Services;

internal sealed class LyricsAggregator : RateLimitedAggregator<ILyricsService>, ILyricsAggregator
{
    public LyricsAggregator(
        IEnumerable<ILyricsService> services,
        IOptions<RateLimitOptions>? rateLimitOptions)
        : base(services, rateLimitOptions)
    {
    }

    public async Task<LyricsDto?> GetLyricsAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(artistName))
            return null;

        IReadOnlyList<LyricsDto> lyrics = await RunAllSafeAsync(
            "lyrics",
            (s, ct) => s.GetLyricsAsync(title, artistName, albumName, duration, ct),
            cancellationToken);

        if (lyrics.Count == 0)
            return null;

        return new LyricsDto
        {
            AlbumName = FirstNonEmpty(lyrics, c => c.AlbumName),
            ArtistName = FirstNonEmpty(lyrics, c => c.ArtistName),
            PlainLyrics = FirstNonEmpty(lyrics, c => c.PlainLyrics),
            SyncLyrics = FirstNonEmpty(lyrics, c => c.SyncLyrics),
            Title = FirstNonEmpty(lyrics, c => c.Title),
            Duration = FirstGreaterThanZero(lyrics, c => c.Duration)
        };
    }


    private static string FirstNonEmpty<T>(IEnumerable<T> items, Func<T, string?> selector) =>
        items.Select(selector).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static int FirstGreaterThanZero<T>(IEnumerable<T> items, Func<T, int?> selector) =>
        items.Select(selector).FirstOrDefault(v => v > 0) ?? default;
}
