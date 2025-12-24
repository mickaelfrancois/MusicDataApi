using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;

namespace MusicData.Infrastructure.Services.LastFm;


public class LastFmService([FromKeyedServices("lastfm")] HttpClient httpClient, JsonSerializerOptions jsonOptions, IOptions<LastFmSettings> settings, ILogger<LastFmService> logger) : IMusicService
{
    private readonly string _apiURL = $"{settings.Value.BaseUrl}?api_key={settings.Value.ApiKey}&format=json";

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
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout);
            if (!entered)
                return null;

            string requestUrl = $"{_apiURL}&method=artist.getinfo&mbid={Uri.EscapeDataString(musicBrainzId)}&lang=en";

            using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            LastFmRootArtist? root = JsonSerializer.Deserialize<LastFmRootArtist>(json, jsonOptions);
            if (root == null || root.Artist == null)
                return null;

            artist = LastFmMapper.Map(root.Artist);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LastFMService.GetArtistAsync: {Message}", ex.Message);
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

        if (string.IsNullOrWhiteSpace(releaseMusicBrainzId))
            return null;

        bool entered = false;
        AlbumDto? album = null;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                return null;

            string requestUrl = $"{_apiURL}&method=album.getinfo&mbid={Uri.EscapeDataString(releaseMusicBrainzId)}&lang=en";

            using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            LastFmRootAlbum? root = JsonSerializer.Deserialize<LastFmRootAlbum>(json, jsonOptions);
            if (root == null || root.Album == null)
                return null;

            album = LastFmMapper.Map(root.Album);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LastFMService.GetAlbumAsync: {Message}", ex.Message);
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


internal sealed class LastFmRootAlbum
{
    public LastFmAlbum Album { get; set; } = default!;
}

internal sealed class LastFmAlbum
{
    public string Name { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public string Mbid { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public List<LastFmImage>? Image { get; set; } = [];

    public string Listeners { get; set; } = string.Empty;

    public string PlayCount { get; set; } = string.Empty;

    public LastFmTracks Tracks { get; set; } = default!;
}

internal sealed class LastFmTracks
{
    public List<LastFmTrack> Track { get; set; } = [];
}

internal sealed class LastFmTrack
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int? Duration { get; set; }

    public LastFmArtist? Artist { get; set; }

    [JsonPropertyName("@attr")]
    public LastFmAttr Attr { get; set; } = default!;
}

internal sealed class LastFmAttr
{
    public int Rank { get; set; }
}

internal sealed class LastFmRootArtist
{
    public LastFmArtist Artist { get; set; } = default!;
}

internal sealed class LastFmArtist
{
    public string Name { get; set; } = string.Empty;

    public string Mbid { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public List<LastFmImage>? Image { get; set; } = null;

    public LastFmSimilarArtists? Similar { get; set; }

    public LastFmBiography? Bio { get; set; }
}

internal sealed class LastFmSimilarArtists
{
    public List<LastFmSimilarArtist> Artist { get; set; } = [];
}

internal sealed class LastFmSimilarArtist
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public List<LastFmImage>? Image { get; set; } = null;
}

internal sealed class LastFmImage
{
    [JsonPropertyName("#text")]
    public string Url { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;
}

internal sealed class LastFmBiography
{
    public string Content { get; set; } = string.Empty;
}

