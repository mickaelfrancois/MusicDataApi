using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.Services.LyricsOvh;

namespace MusicData.Infrastructure.Services.LrcLib;

public class LrcLibService([FromKeyedServices("lrclib")] HttpClient httpClient, JsonSerializerOptions jsonOptions, IOptions<LyricsOvhSettings> settings, ILogger<LyricsOvhService> logger)
    : ILyricsService
{
    public bool Enabled { get; set; } = settings.Value.Enabled;


    public async Task<LyricsDto?> GetLyricsAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrEmpty(artistName) || string.IsNullOrEmpty(albumName))
            return null;

        LyricsDto? lyrics = null;

        try
        {
            LrcLibRootobject? root = await GetLyricsWithDurationAsync(title, artistName, albumName, duration, cancellationToken);

            if (root is null)
                root = await SearchLyricsAsync(title, artistName, albumName, cancellationToken);

            if (root is null)
                return null;

            lyrics = new LyricsDto()
            {
                Title = title,
                ArtistName = artistName,
                AlbumName = albumName,
                PlainLyrics = root.PlainLyrics.Trim(),
                SyncLyrics = root.SyncedLyrics.Trim(),
                Duration = (int)root.Duration,
                Origin = "LrcLib"
            };
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LrcLibService.GetLyricsAsync: {Message}", ex.Message);
            return null;
        }

        return lyrics;
    }


    private async Task<LrcLibRootobject?> GetLyricsWithDurationAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken)
    {
        string requestUrl = $"api/get?artist_name={Uri.EscapeDataString(artistName)}&track_name={Uri.EscapeDataString(title)}&album_name={Uri.EscapeDataString(albumName)}&duration={duration}";

        using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<LrcLibRootobject>(json, jsonOptions);
    }


    private async Task<LrcLibRootobject?> SearchLyricsAsync(string title, string artistName, string albumName, CancellationToken cancellationToken)
    {
        string requestUrl = $"api/search?artist_name={Uri.EscapeDataString(artistName)}&track_name={Uri.EscapeDataString(title)}&album_name={Uri.EscapeDataString(albumName)}";

        using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        LrcLibRootobject[]? lyrics = JsonSerializer.Deserialize<LrcLibRootobject[]>(json, jsonOptions);

        return lyrics?.FirstOrDefault() ?? null;
    }
}


public class LrcLibRootobject
{
    public int Id { get; set; }

    public string TrackName { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string AlbumName { get; set; } = string.Empty;

    public decimal Duration { get; set; }

    public bool Instrumental { get; set; }

    public string PlainLyrics { get; set; } = string.Empty;

    public string SyncedLyrics { get; set; } = string.Empty;
}


public class LrcLibSearchRootobject
{
    public LrcLibRootobject[] Lyrics { get; set; } = Array.Empty<LrcLibRootobject>();
}
