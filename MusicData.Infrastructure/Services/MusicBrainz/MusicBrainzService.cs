using Hqub.MusicBrainz;
using Hqub.MusicBrainz.Entities;
using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Shared;

namespace MusicData.Infrastructure.Services.MusicBrainz;

public partial class MusicBrainzService(HttpClient httpClient, ILogger<MusicBrainzService> logger) : IMusicService, IMusicBrainzLookup
{
    private static readonly SemaphoreSlim _concurrencySemaphore = new(initialCount: 1);
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(10);

    private readonly MusicBrainzClient _client = new(httpClient);


    public async Task<string?> FindArtistAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        bool entered = false;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                throw new TimeoutException("MusicBrainz is busy (concurrency slot not acquired in time).");

            QueryResult<Artist> artists = await _client.Artists.SearchAsync(name.Quote(), 10);

            int count = artists.Items.Count(a => a.Score == 100);

            if (count == 0)
                return null;

            Artist artist = artists.Items.OrderByDescending(a => Levenshtein.Similarity(a.Name, name)).First();

            return artist.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Rethrown: a failed search is not a "no match" and must not become a 404.
            logger.LogError(ex, "MusicBrainzService.FindArtistAsync: {Message}", ex.Message);
            throw;
        }
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }
    }


    public async Task<MusicBrainzReleaseInfo?> FindAlbumAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(albumName) || string.IsNullOrWhiteSpace(artistMusicBrainzId))
            return null;

        bool entered = false;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                throw new TimeoutException("MusicBrainz is busy (concurrency slot not acquired in time).");

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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "FindAlbumAsync: {Message}", ex.Message);
            throw;
        }
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }
    }


    // The Hqub client reports non-success statuses as WebServiceException (never
    // HttpRequestException). 404 = unknown MBID, 400 = malformed MBID: both are a
    // genuine "not found", everything else is a provider failure.
    private static bool IsNotFound(WebServiceException ex) =>
        ex.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.BadRequest;


    public async Task<ArtistDto?> GetArtistAsync(string musicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        bool entered = false;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                throw new TimeoutException("MusicBrainz is busy (concurrency slot not acquired in time).");

            Artist artist = await _client.Artists.GetAsync(musicBrainzId, "artist-rels", "url-rels");

            if (artist == null)
            {
                logger.LogDebug("Artist with MBID '{ArtistMbId}' not found", musicBrainzId);
                return null;
            }

            logger.LogDebug("Retrieved artist '{ArtistName}' (MBID: {ArtistMbId})", artist.Name, artist.Id);

            return MusicBrainzMapper.Map(artist);
        }
        catch (WebServiceException ex) when (IsNotFound(ex))
        {
            logger.LogDebug(ex, "Not found upstream");
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "GetArtistAsync: {Message}", ex.Message);
            throw;
        }
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }
    }


    public async Task<AlbumDto?> GetAlbumAsync(string releaseMusicBrainzId, string? releaseGroupMusicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(releaseMusicBrainzId))
            return null;

        bool entered = false;

        try
        {
            entered = await _concurrencySemaphore.WaitAsync(_waitTimeout, cancellationToken);
            if (!entered)
                throw new TimeoutException("MusicBrainz is busy (concurrency slot not acquired in time).");

            Release release = await _client.Releases.GetAsync(releaseMusicBrainzId, "artist-rels", "url-rels", "release-groups");
            if (release == null)
            {
                logger.LogDebug("Release with MBID '{ReleaseMbId}' not found", releaseMusicBrainzId);
                return null;
            }

            logger.LogDebug("Retrieved release '{ReleaseTitle}' (MBID: {ReleaseMbId})", release.Title, release.Id);

            return MusicBrainzMapper.Map(release);
        }
        catch (WebServiceException ex) when (IsNotFound(ex))
        {
            logger.LogDebug(ex, "Not found upstream");
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "GetAlbumAsync: {Message}", ex.Message);
            throw;
        }
        finally
        {
            if (entered)
                _concurrencySemaphore.Release();
        }
    }
}

