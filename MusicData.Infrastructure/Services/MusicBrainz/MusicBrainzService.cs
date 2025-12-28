using Hqub.MusicBrainz;
using Hqub.MusicBrainz.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Shared;

namespace MusicData.Infrastructure.Services.MusicBrainz;

public partial class MusicBrainzService([FromKeyedServices("musicbrainz")] HttpClient httpClient, ILogger<MusicBrainzService> logger, IOptions<MusicBrainzSettings> settings) : IMusicService
{
    public bool Enabled { get; set; } = settings.Value.Enabled;

    private static readonly SemaphoreSlim _concurrencySemaphore = new(initialCount: 1);
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(10);

    private readonly MusicBrainzClient _client = new(httpClient);


    public async Task<string?> FindArtistAsync(string name, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(name))
            return null;

        bool entered = false;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                return null;

            QueryResult<Artist> artists = await _client.Artists.SearchAsync(name.Quote(), 10);

            int count = artists.Items.Count(a => a.Score == 100);

            if (count == 0)
                return null;

            Artist artist = artists.Items.OrderByDescending(a => Levenshtein.Similarity(a.Name, name)).First();

            return artist.Id;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MusicBrainzService.FindArtistAsync: {Message}", ex.Message);
            return null;
        }
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }
    }


    public async Task<MusicBrainzReleaseInfo?> FindAlbumAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(albumName) || string.IsNullOrWhiteSpace(artistMusicBrainzId))
            return null;

        bool entered = false;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                return null;

            QueryParameters<Release> query = new()
                                    {
                                        { "arid", artistMusicBrainzId },
                                        { "release", albumName.Quote() },
                                        { "type", "album" },
                                        { "status", "official" }
                                    };

            QueryResult<Release> releases = await _client.Releases.SearchAsync(query);

            int count = releases.Items.Where(r => r.Date != null).Count(a => a.Score == 100);

            if (count == 0)
            {
                logger.LogDebug("No matching releases found for album '{AlbumName}' and artist MBID '{ArtistMbId}'", albumName, artistMusicBrainzId);
                return null;
            }

            Release release = releases.Items.OrderByDescending(a => Levenshtein.Similarity(a.Title, albumName)).First();

            logger.LogDebug("Found release '{ReleaseTitle}' (MBID: {ReleaseMbId}) for album '{AlbumName}' and artist MBID '{ArtistMbId}'", release.Title, release.Id, albumName, artistMusicBrainzId);

            return new MusicBrainzReleaseInfo { ReleaseId = release.Id, ReleaseGroupId = release.ReleaseGroup?.Id };
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Operation canceled");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FindAlbumAsync: {Message}", ex.Message);
            return null;
        }
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }
    }


    public async Task<ArtistDto?> GetArtistAsync(string musicBrainzId, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        bool entered = false;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                return null;

            Artist artist = await _client.Artists.GetAsync(musicBrainzId, "artist-rels", "url-rels");

            if (artist == null)
            {
                logger.LogDebug("Artist with MBID '{ArtistMbId}' not found", musicBrainzId);
                return null;
            }

            logger.LogDebug("Retrieved artist '{ArtistName}' (MBID: {ArtistMbId})", artist.Name, artist.Id);

            return MusicBrainzMapper.Map(artist);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogDebug(ex, "Operation canceled");
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
    }


    public async Task<AlbumDto?> GetAlbumAsync(string releaseMusicBrainzId, string? releaseGroupMusicBrainzId, CancellationToken cancellationToken)
    {
        if (!Enabled)
            return null;

        if (string.IsNullOrWhiteSpace(releaseMusicBrainzId))
            return null;

        bool entered = false;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                return null;

            Release release = await _client.Releases.GetAsync(releaseMusicBrainzId, "artist-rels", "url-rels", "release-groups");
            if (release == null)
            {
                logger.LogDebug("Release with MBID '{ReleaseMbId}' not found", releaseMusicBrainzId);
                return null;
            }

            logger.LogDebug("Retrieved release '{ReleaseTitle}' (MBID: {ReleaseMbId})", release.Title, release.Id);

            return MusicBrainzMapper.Map(release);
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
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }
    }
}

