using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.RateLimiting;

namespace MusicData.Infrastructure.Services;

internal class LyricsAggregator : ILyricsAggregator
{
    private readonly IEnumerable<ILyricsService> _services;
    private readonly RateLimitOptions _rateLimits;
    private readonly Dictionary<Type, TokenBucketRateLimiter> _limiters;



    public LyricsAggregator(IEnumerable<ILyricsService> services, IOptions<RateLimitOptions>? rateLimitOptions)
    {
        _services = services;
        _rateLimits = rateLimitOptions!.Value;

        _limiters = _services
            .Select(s => s.GetType())
            .Distinct()
            .ToDictionary(
                t => t,
                t =>
                {
                    string key = t.Name.ToLowerInvariant();
                    if (_rateLimits.ServiceLimits.TryGetValue(key, out (int MaxRequests, int PerSeconds) cfg))

                        return new TokenBucketRateLimiter(cfg.MaxRequests, TimeSpan.FromSeconds(cfg.PerSeconds));

                    return new TokenBucketRateLimiter(1, TimeSpan.FromSeconds(1));
                });
    }

    public async Task<LyricsDto?> GetLyricsAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(title))
            return null;

        if (string.IsNullOrEmpty(artistName))
            return null;

        List<LyricsDto> lyrics = (await GetLyricsInternalAsync(title, artistName, albumName, duration, cancellationToken)).ToList();
        if (lyrics.Count == 0)
            return null;

        LyricsDto merged = new()
        {
            AlbumName = FirstNonEmpty(lyrics, c => c.AlbumName),
            ArtistName = FirstNonEmpty(lyrics, c => c.ArtistName),
            PlainLyrics = FirstNonEmpty(lyrics, c => c.PlainLyrics),
            SyncLyrics = FirstNonEmpty(lyrics, c => c.SyncLyrics),
            Title = FirstNonEmpty(lyrics, c => c.Title),
            Duration = FirstGreaterThanZero(lyrics, c => c.Duration)
        };

        return merged;
    }


    private async Task<IEnumerable<LyricsDto>> GetLyricsInternalAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken)
    {

        Task<LyricsDto?>[] tasks = _services.Select(s => SafeGetArtistAsync(s, title, artistName, albumName, duration, cancellationToken)).ToArray();
        LyricsDto?[] results = await Task.WhenAll(tasks);

        return results.Where(r => r is not null)!;
    }


    private async Task<LyricsDto?> SafeGetArtistAsync(ILyricsService service, string title, string artistName, string albumName, int duration, CancellationToken cancellationToken)
    {
        try
        {
            if (_limiters.TryGetValue(service.GetType(), out TokenBucketRateLimiter? limiter))
                await limiter.WaitForAvailabilityAsync();

            return await service.GetLyricsAsync(title, artistName, albumName, duration, cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }


    private static string FirstNonEmpty<T>(IEnumerable<T> items, Func<T, string?> selector) =>
        items.Select(selector).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static int FirstGreaterThanZero<T>(IEnumerable<T> items, Func<T, int?> selector) =>
       items.Select(selector).FirstOrDefault(v => v > 0) ?? default;
}
