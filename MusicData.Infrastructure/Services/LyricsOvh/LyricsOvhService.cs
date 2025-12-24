using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;

namespace MusicData.Infrastructure.Services.LyricsOvh;

public class LyricsOvhService([FromKeyedServices("lyricsovh")] HttpClient httpClient, JsonSerializerOptions jsonOptions, IOptions<LyricsOvhSettings> settings, ILogger<LyricsOvhService> logger)
    : ILyricsService
{
    public bool Enabled { get; set; } = settings.Value.Enabled;


    public async Task<LyricsDto?> GetLyricsAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrEmpty(artistName))
            return null;

        LyricsDto? lyrics = null;

        try
        {
            string requestUrl = $"{Uri.EscapeDataString(artistName)}/{Uri.EscapeDataString(title)}";

            using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            LyricsOvhRootobject? root = JsonSerializer.Deserialize<LyricsOvhRootobject>(json, jsonOptions);

            if (root == null || root.Lyrics == null)
                return null;

            lyrics = new LyricsDto()
            {
                Title = title,
                ArtistName = artistName,
                PlainLyrics = root.Lyrics.Trim(),
                Origin = "LyricsOvh"
            };
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LyricsOvhService.GetLyricsAsync: {Message}", ex.Message);
            return null;
        }

        return lyrics;
    }
}

public class LyricsOvhRootobject
{
    public string Lyrics { get; set; } = string.Empty;
}
