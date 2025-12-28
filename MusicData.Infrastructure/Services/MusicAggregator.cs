using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.RateLimiting;
using MusicData.Infrastructure.Services.MusicBrainz;
using static MusicData.Infrastructure.Services.MusicBrainz.MusicBrainzService;

namespace MusicData.Infrastructure.Services;

public class MusicAggregator : IMusicAggregator
{
    private readonly IEnumerable<IMusicService> _services;
    private readonly RateLimitOptions _rateLimits;
    private readonly Dictionary<Type, TokenBucketRateLimiter> _limiters;

    public MusicAggregator(IEnumerable<IMusicService> services, IOptions<RateLimitOptions>? rateLimitOptions)
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
                    if (_rateLimits.ServiceLimits.TryGetValue(key, out (int MaxRequests, int PerMilliSeconds) cfg))

                        return new TokenBucketRateLimiter(cfg.MaxRequests, TimeSpan.FromMilliseconds(cfg.PerMilliSeconds));

                    return new TokenBucketRateLimiter(1, TimeSpan.FromMilliseconds(1));
                });
    }


    public async Task<ArtistDto?> GetArtistByNameAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        MusicBrainzService musicBrainz = _services.OfType<MusicBrainzService>().FirstOrDefault()!;
        string? musicBrainzId = await musicBrainz.FindArtistAsync(name, cancellationToken);

        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        List<ArtistDto> artists = (await GetArtistsAsync(musicBrainzId, cancellationToken)).ToList();
        if (artists.Count == 0)
            return null;

        return BuildMergedArtist(artists);
    }


    public async Task<ArtistDto?> GetArtistByMusicBrainzIdAsync(string musicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        List<ArtistDto> artists = (await GetArtistsAsync(musicBrainzId, cancellationToken)).ToList();
        if (artists.Count == 0)
            return null;

        return BuildMergedArtist(artists);
    }


    private async Task<IEnumerable<ArtistDto>> GetArtistsAsync(string musicBrainzId, CancellationToken cancellationToken)
    {
        Task<ArtistDto?>[] tasks = _services.Select(service => SafeGetArtistAsync(service, musicBrainzId, cancellationToken)).ToArray();
        ArtistDto?[] results = await Task.WhenAll(tasks);

        return results.Where(artist => artist is not null)!;
    }


    public async Task<AlbumDto?> GetAlbumByNameAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumName))
            return null;

        List<AlbumDto> albums = (await GetAlbumsAsync(albumName, artistMusicBrainzId, cancellationToken)).ToList();
        if (albums.Count == 0)
            return null;

        AlbumDto mergedAlbums = BuildMergedAlbum(albums);
        mergedAlbums.MusicBrainzArtistID = artistMusicBrainzId;

        return mergedAlbums;
    }


    public async Task<AlbumDto?> GetAlbumByMusicBrainzIdsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumMusicBrainzId) || string.IsNullOrWhiteSpace(artistMusicBrainzId))
            return null;

        List<AlbumDto> albums = (await GetAlbumsAsync(albumMusicBrainzId, cancellationToken)).ToList();
        if (albums.Count == 0)
            return null;

        AlbumDto mergedAlbums = BuildMergedAlbum(albums);
        mergedAlbums.MusicBrainzArtistID = artistMusicBrainzId;

        return mergedAlbums;
    }


    private async Task<IEnumerable<AlbumDto>> GetAlbumsAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken)
    {
        MusicBrainzService musicBrainz = _services.OfType<MusicBrainzService>().FirstOrDefault()!;
        MusicBrainzReleaseInfo? releaseInfo = await musicBrainz.FindAlbumAsync(albumName, artistMusicBrainzId, cancellationToken);

        if (releaseInfo is null)
            return [];

        Task<AlbumDto?>[] tasks = _services
            .Select(s => SafeGetAlbumAsync(s, releaseInfo.ReleaseId, releaseInfo.ReleaseGroupId, cancellationToken))
            .ToArray();

        AlbumDto?[] results = await Task.WhenAll(tasks);

        return results.Where(r => r is not null)!;
    }


    private async Task<IEnumerable<AlbumDto>> GetAlbumsAsync(string musicBrainzId, CancellationToken cancellationToken)
    {
        Task<AlbumDto?>[] tasks = _services
            .Select(s => SafeGetAlbumAsync(s, musicBrainzId, relaseGroupMusicBrainzId: null, cancellationToken))
            .ToArray();

        AlbumDto?[] results = await Task.WhenAll(tasks);

        return results.Where(r => r is not null)!;
    }


    private async Task<ArtistDto?> SafeGetArtistAsync(IMusicService service, string musicBrainzId, CancellationToken cancellationToken)
    {
        try
        {
            if (_limiters.TryGetValue(service.GetType(), out TokenBucketRateLimiter? limiter))
                await limiter.WaitForAvailabilityAsync();

            return await service.GetArtistAsync(musicBrainzId, cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }


    private async Task<AlbumDto?> SafeGetAlbumAsync(IMusicService service, string? releaseMusicBrainzId, string? relaseGroupMusicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(releaseMusicBrainzId))
            return null;

        try
        {
            if (_limiters.TryGetValue(service.GetType(), out TokenBucketRateLimiter? limiter))
                await limiter.WaitForAvailabilityAsync();

            return await service.GetAlbumAsync(releaseMusicBrainzId, relaseGroupMusicBrainzId, cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }


    private static ArtistDto BuildMergedArtist(List<ArtistDto> artists)
    {
        ArtistDto merged = new()
        {
            Name = FirstNonEmpty(artists, a => a.Name),
            MusicBrainzID = FirstNonEmpty(artists, a => a.MusicBrainzID),
            LastFM = FirstNonEmpty(artists, a => a.LastFM),
            PictureUrl = FirstNonEmpty(artists, a => a.PictureUrl),
            Biography = FirstNonEmpty(artists, a => a.Biography),
            AllMusic = FirstNonEmpty(artists, a => a.AllMusic),
            AudioDbID = FirstNonEmpty(artists, a => a.AudioDbID),
            Bandsintown = FirstNonEmpty(artists, a => a.Bandsintown),
            BannerUrl = FirstNonEmpty(artists, a => a.BannerUrl),
            CountryCode = FirstNonEmpty(artists, a => a.CountryCode),
            Discogs = FirstNonEmpty(artists, a => a.Discogs),
            Facebook = FirstNonEmpty(artists, a => a.Facebook),
            FanartUrl = FirstNonEmpty(artists, a => a.FanartUrl),
            Fanart2Url = FirstNonEmpty(artists, a => a.Fanart2Url),
            Fanart3Url = FirstNonEmpty(artists, a => a.Fanart3Url),
            Fanart4Url = FirstNonEmpty(artists, a => a.Fanart4Url),
            Fanart5Url = FirstNonEmpty(artists, a => a.Fanart5Url),
            Flickr = FirstNonEmpty(artists, a => a.Flickr),
            BeginYear = FirstGreaterThanZero(artists, a => a.BeginYear),
            Instagram = FirstNonEmpty(artists, a => a.Instagram),
            LogoUrl = FirstNonEmpty(artists, a => a.LogoUrl),
            Twitter = FirstNonEmpty(artists, a => a.Twitter),
            Website = FirstNonEmpty(artists, a => a.Website),
            Youtube = FirstNonEmpty(artists, a => a.Youtube),
            EndYear = FirstGreaterThanZero(artists, a => a.EndYear),
            Disbanded = artists.Any(a => a.Disbanded),
            Wikipedia = FirstNonEmpty(artists, a => a.Wikipedia),
            Imdb = FirstNonEmpty(artists, a => a.Imdb),
            SongKick = FirstNonEmpty(artists, a => a.SongKick),
            SoundCloud = FirstNonEmpty(artists, a => a.SoundCloud),
            Threads = FirstNonEmpty(artists, a => a.Threads),
            TikTok = FirstNonEmpty(artists, a => a.TikTok),

            Members = artists.SelectMany(a => a.Members ?? Enumerable.Empty<MemberDto>())
                           .GroupBy(m => m.Name)
                           .Select(g => g.First())
                           .ToList()
        };

        return merged;
    }


    private static AlbumDto BuildMergedAlbum(List<AlbumDto> albums)
    {
        AlbumDto merged = new()
        {
            Origin = "Aggregated",
            Name = FirstNonEmpty(albums, c => c.Name),
            Genre = FirstNonEmpty(albums, c => c.Genre),
            Artist = FirstNonEmpty(albums, c => c.Artist),
            Label = FirstNonEmpty(albums, c => c.Label),
            ReleaseFormat = FirstNonEmpty(albums, c => c.ReleaseFormat),
            AllMusicID = FirstNonEmpty(albums, c => c.AllMusicID),
            AmazonID = FirstNonEmpty(albums, c => c.AmazonID),
            AudioDbArtistID = FirstNonEmpty(albums, c => c.AudioDbArtistID),
            AudioDbID = FirstNonEmpty(albums, c => c.AudioDbID),
            Biography = FirstNonEmpty(albums, c => c.Biography),
            DiscogsID = FirstNonEmpty(albums, c => c.DiscogsID),
            GeniusID = FirstNonEmpty(albums, c => c.GeniusID),
            LyricWikiID = FirstNonEmpty(albums, c => c.LyricWikiID),
            MusicBrainzArtistID = FirstNonEmpty(albums, c => c.MusicBrainzArtistID),
            MusicBrainzID = FirstNonEmpty(albums, c => c.MusicBrainzID),
            MusicMozID = FirstNonEmpty(albums, c => c.MusicMozID),
            PictureUrl = FirstNonEmpty(albums, c => c.PictureUrl),
            ReleaseGroupMusicBrainzID = FirstNonEmpty(albums, c => c.ReleaseGroupMusicBrainzID),
            Sales = FirstNonEmpty(albums, c => c.Sales),
            WikidataID = FirstNonEmpty(albums, c => c.WikidataID),
            Wikipedia = FirstNonEmpty(albums, c => c.Wikipedia),
            WikipediaID = FirstNonEmpty(albums, c => c.WikipediaID),
            Year = FirstNonEmpty(albums, c => c.Year),
            Score = FirstGreaterThanZero(albums, c => c.Score),
            ReleaseDate = FirstNonNull(albums, c => c.ReleaseDate),
            LastFM = FirstNonEmpty(albums, c => c.LastFM)
        };

        List<TrackDto>? tracks = albums.FirstOrDefault(c => c.Tracks?.Count > 0)?.Tracks;
        if (tracks is not null)
        {
            foreach (TrackDto track in tracks)
            {
                TrackDto trackDto = new()
                {
                    Duration = track.Duration,
                    Name = track.Name,
                    Position = track.Position,
                };

                merged.Tracks.Add(trackDto);
            }
        }

        return merged;
    }


    private static DateTime? FirstNonNull<T>(IEnumerable<T> items, Func<T, DateTime?> selector) =>
        items.Select(selector).FirstOrDefault(v => v is not null) ?? null;

    private static string FirstNonEmpty<T>(IEnumerable<T> items, Func<T, string?> selector) =>
        items.Select(selector).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

    private static int? FirstGreaterThanZero<T>(IEnumerable<T> items, Func<T, int?> selector) =>
        items.Select(selector).FirstOrDefault(v => v.HasValue && v > 0) ?? null;
}