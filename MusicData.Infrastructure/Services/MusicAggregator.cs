using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.RateLimiting;
using MusicData.Infrastructure.Services.MusicBrainz;

namespace MusicData.Infrastructure.Services;

internal sealed class MusicAggregator : RateLimitedAggregator<IMusicService>, IMusicAggregator
{
    private readonly IMusicBrainzLookup _musicBrainz;

    public MusicAggregator(
        IEnumerable<IMusicService> services,
        IMusicBrainzLookup musicBrainz,
        IOptions<RateLimitOptions>? rateLimitOptions)
        : base(services, rateLimitOptions)
    {
        _musicBrainz = musicBrainz;
    }


    public async Task<ArtistDto?> GetArtistByNameAsync(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string? musicBrainzId = await _musicBrainz.FindArtistAsync(name, cancellationToken);
        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        return await GetArtistByMusicBrainzIdAsync(musicBrainzId, cancellationToken);
    }


    public async Task<ArtistDto?> GetArtistByMusicBrainzIdAsync(string musicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(musicBrainzId))
            return null;

        IReadOnlyList<ArtistDto> artists = await RunAllSafeAsync(
            "artist",
            (s, ct) => s.GetArtistAsync(musicBrainzId, ct),
            cancellationToken);

        return artists.Count == 0 ? null : BuildMergedArtist(artists);
    }


    public async Task<AlbumDto?> GetAlbumByNameAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumName))
            return null;

        MusicBrainzReleaseInfo? releaseInfo = await _musicBrainz.FindAlbumAsync(albumName, artistMusicBrainzId, cancellationToken);
        if (releaseInfo is null || string.IsNullOrEmpty(releaseInfo.ReleaseId))
            return null;

        string releaseId = releaseInfo.ReleaseId;
        string? releaseGroupId = releaseInfo.ReleaseGroupId;

        IReadOnlyList<AlbumDto> albums = await RunAllSafeAsync(
            "album",
            (s, ct) => s.GetAlbumAsync(releaseId, releaseGroupId, ct),
            cancellationToken);

        if (albums.Count == 0)
            return null;

        AlbumDto merged = BuildMergedAlbum(albums);
        merged.MusicBrainzArtistID = artistMusicBrainzId;
        return merged;
    }


    public async Task<AlbumDto?> GetAlbumByMusicBrainzIdsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(albumMusicBrainzId) || string.IsNullOrWhiteSpace(artistMusicBrainzId))
            return null;

        IReadOnlyList<AlbumDto> albums = await RunAllSafeAsync(
            "album",
            (s, ct) => s.GetAlbumAsync(albumMusicBrainzId, null, ct),
            cancellationToken);

        if (albums.Count == 0)
            return null;

        AlbumDto merged = BuildMergedAlbum(albums);
        merged.MusicBrainzArtistID = artistMusicBrainzId;
        return merged;
    }


    private static ArtistDto BuildMergedArtist(IReadOnlyList<ArtistDto> artists)
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


    private static AlbumDto BuildMergedAlbum(IReadOnlyList<AlbumDto> albums)
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
                merged.Tracks.Add(new TrackDto { Duration = track.Duration, Name = track.Name, Position = track.Position });
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
