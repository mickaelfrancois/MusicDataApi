using Microsoft.Extensions.Logging.Abstractions;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Lyrics;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;
using NSubstitute;

namespace MusicData.Tests.Application.Features;

public class GetLyricsTests
{
    private readonly ILyricsRepository _repo = Substitute.For<ILyricsRepository>();
    private readonly ILyricsAggregator _aggregator = Substitute.For<ILyricsAggregator>();
    private readonly IKeyedLocker _locker = Substitute.For<IKeyedLocker>();
    private readonly GetLyrics _sut;

    public GetLyricsTests()
    {
        _locker.LockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult<IDisposable>(new NoopDisposable()));

        _sut = new GetLyrics(_repo, _aggregator, _locker, NullLogger<GetLyrics>.Instance);
    }

    [Fact]
    public async Task NotFoundExternally_PersistsNegativeCacheMarker_AndReturnsNull()
    {
        _repo.Get("Walk", "Foo Fighters").Returns((LyricsEntity?)null);
        _aggregator.GetLyricsAsync("Walk", "Foo Fighters", "", 0, Arg.Any<CancellationToken>())
                   .Returns((LyricsDto?)null);

        LyricsDto? result = await _sut.HandleAsync("Walk", "Foo Fighters", "", 0);

        Assert.Null(result);
        _repo.Received(1).Add(Arg.Is<LyricsEntity>(e =>
            e.Title == "Walk" && e.ArtistName == "Foo Fighters"));
    }

    [Fact]
    public async Task CacheHit_ShortCircuits_AndDoesNotCallAggregator()
    {
        _repo.Get("Walk", "Foo Fighters")
             .Returns(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters", PlainLyrics = "..." });

        LyricsDto? result = await _sut.HandleAsync("Walk", "Foo Fighters", "", 0);

        Assert.NotNull(result);
        Assert.Equal("Cache", result!.Origin);
        await _aggregator.DidNotReceive().GetLyricsAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CacheMiss_AggregatorReturnsLyrics_PersistsAndReturns()
    {
        _repo.Get("Walk", "Foo Fighters").Returns((LyricsEntity?)null);
        LyricsDto fromAgg = new() { Title = "Walk", ArtistName = "Foo Fighters", PlainLyrics = "...", Origin = "LrcLib" };
        _aggregator.GetLyricsAsync("Walk", "Foo Fighters", "Wasting Light", 257, Arg.Any<CancellationToken>())
                   .Returns(fromAgg);

        LyricsDto? result = await _sut.HandleAsync("Walk", "Foo Fighters", "Wasting Light", 257);

        Assert.Same(fromAgg, result);
        _repo.Received(1).Add(Arg.Any<LyricsEntity>());
    }


    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
