using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Exceptions;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.RateLimiting;
using MusicData.Infrastructure.Services;
using MusicData.Infrastructure.Services.MusicBrainz;

namespace MusicData.Tests.Infrastructure.Services;

public class MusicAggregatorTests
{
    private const string ArtistMbid = "67f66c07-6e61-4026-ade5-7e782fad3a5d";
    private const string AlbumMbid = "98cfdc56-7acf-4ecb-8b42-71c5b1c7421b";

    [Fact]
    public async Task Artist_MusicBrainzFailed_IsUnavailable_EvenWhenASecondaryAnswered()
    {
        FakeMusicBrainz musicBrainz = new() { Artist = _ => throw new HttpRequestException("503") };
        FakeService fanart = new() { Artist = _ => new ArtistDto { FanartUrl = "https://img/1.jpg" } };

        await Assert.ThrowsAsync<ExternalServicesUnavailableException>(
            () => Create(musicBrainz, fanart).GetArtistByMusicBrainzIdAsync(ArtistMbid, CancellationToken.None));
    }

    [Fact]
    public async Task Artist_UnknownToMusicBrainz_IsNotFound_EvenWhenASecondaryAnswered()
    {
        FakeMusicBrainz musicBrainz = new() { Artist = _ => null };
        FakeService lastFm = new() { Artist = _ => new ArtistDto { Biography = "bio" } };

        Assert.Null(await Create(lastFm, musicBrainz).GetArtistByMusicBrainzIdAsync(ArtistMbid, CancellationToken.None));
    }

    [Fact]
    public async Task Artist_SecondaryFailed_ReturnsMusicBrainzIdentity_FlaggedPartial()
    {
        FakeMusicBrainz musicBrainz = new() { Artist = id => new ArtistDto { Name = "Foo Fighters", MusicBrainzID = id } };
        FakeService lastFm = new() { Artist = _ => throw new TimeoutException() };

        ArtistDto? artist = await Create(lastFm, musicBrainz).GetArtistByMusicBrainzIdAsync(ArtistMbid, CancellationToken.None);

        Assert.NotNull(artist);
        Assert.Equal("Foo Fighters", artist!.Name);
        Assert.Equal(ArtistMbid, artist.MusicBrainzID);
        Assert.True(artist.IsPartial);
    }

    [Fact]
    public async Task Artist_AllAnswered_IsNotPartial()
    {
        FakeMusicBrainz musicBrainz = new() { Artist = id => new ArtistDto { Name = "Foo Fighters", MusicBrainzID = id } };
        FakeService fanart = new() { Artist = _ => null };

        ArtistDto? artist = await Create(musicBrainz, fanart).GetArtistByMusicBrainzIdAsync(ArtistMbid, CancellationToken.None);

        Assert.False(artist!.IsPartial);
    }

    [Fact]
    public async Task Album_MusicBrainzFailed_IsUnavailable_EvenWhenASecondaryAnswered()
    {
        FakeMusicBrainz musicBrainz = new() { Album = (_, _) => throw new HttpRequestException("503") };
        FakeService coverArt = new() { Album = (_, _) => new AlbumDto { PictureUrl = "https://img/cover.jpg" } };

        await Assert.ThrowsAsync<ExternalServicesUnavailableException>(
            () => Create(musicBrainz, coverArt).GetAlbumByMusicBrainzIdAsync(AlbumMbid, ArtistMbid, CancellationToken.None));
    }

    [Fact]
    public async Task Album_UnknownToMusicBrainz_IsNotFound_EvenWhenASecondaryAnswered()
    {
        FakeMusicBrainz musicBrainz = new() { Album = (_, _) => null };
        FakeService coverArt = new() { Album = (_, _) => new AlbumDto { PictureUrl = "https://img/cover.jpg" } };

        Assert.Null(await Create(musicBrainz, coverArt).GetAlbumByMusicBrainzIdAsync(AlbumMbid, ArtistMbid, CancellationToken.None));
    }

    [Fact]
    public async Task ArtistByName_LookupFailed_IsUnavailable_NotNotFound()
    {
        FakeMusicBrainz musicBrainz = new() { FindArtist = _ => throw new TimeoutException("busy") };

        await Assert.ThrowsAsync<ExternalServicesUnavailableException>(
            () => Create(musicBrainz).GetArtistByNameAsync("Foo Fighters", CancellationToken.None));
    }

    [Fact]
    public async Task AlbumByName_LookupFailed_IsUnavailable_NotNotFound()
    {
        FakeMusicBrainz musicBrainz = new() { FindAlbum = (_, _) => throw new HttpRequestException("503") };

        await Assert.ThrowsAsync<ExternalServicesUnavailableException>(
            () => Create(musicBrainz).GetAlbumByNameAsync("Wasting Light", ArtistMbid, CancellationToken.None));
    }

    [Fact]
    public async Task ArtistByName_NoMatch_IsNotFound()
    {
        FakeMusicBrainz musicBrainz = new() { FindArtist = _ => null };

        Assert.Null(await Create(musicBrainz).GetArtistByNameAsync("zzzz", CancellationToken.None));
    }

    [Fact]
    public async Task ArtistByName_Lookup_GoesThroughTheMusicBrainzTokenBucket()
    {
        // One token per 400 ms: the lookup takes the first one, so the artist
        // fetch that follows has to wait for the refill.
        RateLimitOptions limits = new()
        {
            ServiceLimits = { ["fakemusicbrainz"] = new ServiceRateLimit { MaxRequests = 1, PerMilliSeconds = 400 } }
        };
        FakeMusicBrainz musicBrainz = new()
        {
            FindArtist = _ => ArtistMbid,
            Artist = id => new ArtistDto { Name = "Foo Fighters", MusicBrainzID = id }
        };
        MusicAggregator sut = new([musicBrainz], musicBrainz, Options.Create(limits));

        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        ArtistDto? artist = await sut.GetArtistByNameAsync("Foo Fighters", CancellationToken.None);

        Assert.NotNull(artist);
        Assert.True(System.Diagnostics.Stopwatch.GetElapsedTime(started) >= TimeSpan.FromMilliseconds(300));
    }


    private static MusicAggregator Create(params IMusicService[] services) => new(
        services,
        services.OfType<IMusicBrainzLookup>().Single(),
        Options.Create(new RateLimitOptions()));


    internal class FakeService : IMusicService
    {
        public Func<string, ArtistDto?> Artist { get; init; } = _ => null;

        public Func<string, string?, AlbumDto?> Album { get; init; } = (_, _) => null;

        public List<(string ReleaseId, string? ReleaseGroupId)> AlbumCalls { get; } = [];

        public Task<ArtistDto?> GetArtistAsync(string musicBrainzId, CancellationToken cancellationToken)
        {
            try { return Task.FromResult(Artist(musicBrainzId)); }
            catch (Exception ex) { return Task.FromException<ArtistDto?>(ex); }
        }

        public Task<AlbumDto?> GetAlbumAsync(string releaseMusicBrainzId, string? releaseGroupMusicBrainzId, CancellationToken cancellationToken)
        {
            AlbumCalls.Add((releaseMusicBrainzId, releaseGroupMusicBrainzId));
            try { return Task.FromResult(Album(releaseMusicBrainzId, releaseGroupMusicBrainzId)); }
            catch (Exception ex) { return Task.FromException<AlbumDto?>(ex); }
        }
    }

    internal sealed class FakeMusicBrainz : FakeService, IMusicBrainzLookup
    {
        public Func<string, string?> FindArtist { get; init; } = _ => null;

        public Func<string, string, MusicBrainzReleaseInfo?> FindAlbum { get; init; } = (_, _) => null;

        public Task<string?> FindArtistAsync(string name, CancellationToken cancellationToken)
        {
            try { return Task.FromResult(FindArtist(name)); }
            catch (Exception ex) { return Task.FromException<string?>(ex); }
        }

        public Task<MusicBrainzReleaseInfo?> FindAlbumAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken)
        {
            try { return Task.FromResult(FindAlbum(albumName, artistMusicBrainzId)); }
            catch (Exception ex) { return Task.FromException<MusicBrainzReleaseInfo?>(ex); }
        }
    }
}
