using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;

namespace MusicData.Infrastructure.Services.CoverArt;


public class CoverArtService(HttpClient httpClient, JsonSerializerOptions jsonOptions, IOptions<CoverArtSettings> settings, ILogger<CoverArtService> logger)
    : IMusicService
{
    private readonly string _apiURL = $"{settings.Value.BaseUrl}/release";

    public bool Enabled { get; set; } = settings.Value.Enabled;


    public Task<ArtistDto?> GetArtistAsync(string musicBrainzId, CancellationToken cancellationToken)
    {
        return Task.FromResult<ArtistDto?>(null);
    }


    public async Task<AlbumDto?> GetAlbumAsync(string releaseMusicBrainzId, string? releaseGroupMusicBrainzId, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(releaseMusicBrainzId))
            return null;

        try
        {
            string requestUrl = $"{_apiURL}/{Uri.EscapeDataString(releaseMusicBrainzId)}";

            using HttpResponseMessage response = await httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("CovertArt response with {StatusCode}", response.StatusCode);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            CoverArtRootObject? root = JsonSerializer.Deserialize<CoverArtRootObject>(json, jsonOptions);
            if (root == null || root.Images == null || root.Images.Length == 0)
            {
                logger.LogDebug("No album result found");
                return null;
            }

            logger.LogDebug("Album found: {Album}", releaseMusicBrainzId);
            return CoverArtMapper.Map(root);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Operation canceled");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetAlbumAsync: {Message}", ex.Message);
            return null;
        }
    }
}


public class CoverArtRootObject
{
    public CoverArtImage[]? Images { get; set; }

    public string? Release { get; set; }
}

public class CoverArtImage
{
    public bool Front { get; set; }

    public bool Back { get; set; }

    public string? Image { get; set; }

    public bool Approved { get; set; }
}
