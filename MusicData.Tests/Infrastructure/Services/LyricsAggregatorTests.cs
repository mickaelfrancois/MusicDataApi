using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Application.Exceptions;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.RateLimiting;
using MusicData.Infrastructure.Services;

namespace MusicData.Tests.Infrastructure.Services;

public class LyricsAggregatorTests
{
    [Fact]
    public async Task OneProviderHasLyrics_AnotherFailed_ReturnsLyricsFlaggedPartial()
    {
        LyricsAggregator sut = Create(
            new FakeLyrics(() => throw new HttpRequestException("502")),
            new FakeLyrics(() => new LyricsDto { Title = "Walk", ArtistName = "Foo", PlainLyrics = "la la" }));

        LyricsDto? lyrics = await sut.GetLyricsAsync("Walk", "Foo", "", 0, CancellationToken.None);

        Assert.Equal("la la", lyrics!.PlainLyrics);
        Assert.True(lyrics.IsPartial);
    }

    [Fact]
    public async Task AllProvidersAnswered_IsNotPartial()
    {
        LyricsAggregator sut = Create(
            new FakeLyrics(() => null),
            new FakeLyrics(() => new LyricsDto { Title = "Walk", ArtistName = "Foo", PlainLyrics = "la la" }));

        Assert.False((await sut.GetLyricsAsync("Walk", "Foo", "", 0, CancellationToken.None))!.IsPartial);
    }

    [Fact]
    public async Task NoLyrics_AndAProviderFailed_IsUnavailable_NotNotFound()
    {
        LyricsAggregator sut = Create(
            new FakeLyrics(() => throw new HttpRequestException("502")),
            new FakeLyrics(() => null));

        await Assert.ThrowsAsync<ExternalServicesUnavailableException>(
            () => sut.GetLyricsAsync("Walk", "Foo", "", 0, CancellationToken.None));
    }


    private static LyricsAggregator Create(params ILyricsService[] services) =>
        new(services, Options.Create(new RateLimitOptions()));

    private sealed class FakeLyrics(Func<LyricsDto?> body) : ILyricsService
    {
        public Task<LyricsDto?> GetLyricsAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken)
        {
            try { return Task.FromResult(body()); }
            catch (Exception ex) { return Task.FromException<LyricsDto?>(ex); }
        }
    }
}
