using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;

namespace MusicData.Infrastructure.Services.Fanart;


public class FanartService([FromKeyedServices("fanart")] HttpClient httpClient, JsonSerializerOptions jsonOptions, IOptions<FanartSettings> settings, ILogger<FanartService> logger)
    : IMusicService
{
    public bool Enabled { get; set; } = settings.Value.Enabled;

    private static readonly SemaphoreSlim _concurrencySemaphore = new(initialCount: 5);
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(10);

    public async Task<ArtistDto?> GetArtistAsync(string musicBrainzId, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        bool entered = false;
        ArtistDto? artist = null;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                return null;

            string requestUrl = $"{musicBrainzId}?api_key={settings.Value.ApiKey}";

            using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("FanartTv response with {StatusCode}", response.StatusCode);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            FanartRoot? root = JsonSerializer.Deserialize<FanartRoot>(json, jsonOptions);
            if (root == null)
            {
                logger.LogDebug("Artist '{MusicBrainzId}' not found", musicBrainzId);
                return null;
            }

            logger.LogDebug("Found Artist '{MusicBrainzId}'", musicBrainzId);

            artist = FanartMapper.MapArtist(root);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Operation canceled in GetArtistAsync");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetArtistAsync: {Message}", ex.Message);
            return null;
        }
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }

        return artist;
    }


    public async Task<AlbumDto?> GetAlbumAsync(string releaseMusicBrainzId, string? releaseGroupMusicBrainzId, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(releaseGroupMusicBrainzId))
            return null;

        bool entered = false;
        AlbumDto? album = null;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                return null;

            string requestUrl = $"{releaseGroupMusicBrainzId}?api_key={settings.Value.ApiKey}";

            using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("FanartTv response with {StatusCode}", response.StatusCode);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            FanartRoot? root = JsonSerializer.Deserialize<FanartRoot>(json, jsonOptions);
            if (root == null)
            {
                logger.LogDebug("Album '{ReleaseGroupMusicBrainzId}' not found", releaseGroupMusicBrainzId);
                return null;
            }

            logger.LogDebug("Found Album '{ReleaseGroupMusicBrainzId}'", releaseGroupMusicBrainzId);

            album = FanartMapper.MapAlbum(root);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Operation canceled in GetArtistAsync");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FanartService.GetAlbumAsync: {Message}", ex.Message);
            return null;
        }
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }

        return album;
    }
}



internal sealed class FanartRoot
{
    public List<FanartImage> ArtistBackground { get; set; } = default!;

    public List<FanartImage> ArtistThumb { get; set; } = default!;

    public List<FanartImage> MusicLogo { get; set; } = default!;

    public List<FanartImage> MusicBanner { get; set; } = default!;
}

internal sealed class FanartImage
{
    public string Url { get; set; } = string.Empty;

    public string Likes { get; set; } = string.Empty;

    public int Score
    {
        get
        {
            if (string.IsNullOrEmpty(Likes))
                return 0;
            Int32.TryParse(Likes, out int score);
            return score;
        }
    }
}


