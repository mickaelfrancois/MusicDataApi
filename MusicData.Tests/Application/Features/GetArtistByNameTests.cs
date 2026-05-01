using Microsoft.Extensions.Logging.Abstractions;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Artists;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;
using NSubstitute;

namespace MusicData.Tests.Application.Features;

public class GetArtistByNameTests
{
    private readonly IArtistRepository _repo = Substitute.For<IArtistRepository>();
    private readonly IMusicAggregator _aggregator = Substitute.For<IMusicAggregator>();
    private readonly IKeyedLocker _locker = Substitute.For<IKeyedLocker>();
    private readonly GetArtistByName _sut;

    public GetArtistByNameTests()
    {
        _locker.LockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult<IDisposable>(new NoopDisposable()));

        _sut = new GetArtistByName(_repo, _aggregator, _locker, NullLogger<GetArtistByName>.Instance);
    }

    [Fact]
    public async Task EmptyName_ReturnsNull_WithoutTouchingDependencies()
    {
        ArtistDto? result = await _sut.HandleAsync("");

        Assert.Null(result);
        _repo.DidNotReceive().GetByName(Arg.Any<string>());
        await _aggregator.DidNotReceive().GetArtistByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CacheHit_ReturnsCachedDto_WithOriginCache_AndDoesNotCallAggregator()
    {
        ArtistEntity entity = new() { Name = "Foo Fighters", MusicBrainzID = "id" };
        _repo.GetByName("Foo Fighters").Returns(entity);

        ArtistDto? result = await _sut.HandleAsync("Foo Fighters");

        Assert.NotNull(result);
        Assert.Equal("Foo Fighters", result!.Name);
        Assert.Equal("Cache", result.Origin);
        await _aggregator.DidNotReceive().GetArtistByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _repo.DidNotReceive().Add(Arg.Any<ArtistEntity>());
    }

    [Fact]
    public async Task CacheMiss_CallsAggregator_PersistsResult_AndReturnsIt()
    {
        _repo.GetByName("Foo Fighters").Returns((ArtistEntity?)null);
        ArtistDto fromAggregator = new() { Name = "Foo Fighters", MusicBrainzID = "id" };
        _aggregator.GetArtistByNameAsync("Foo Fighters", Arg.Any<CancellationToken>())
                   .Returns(fromAggregator);

        ArtistDto? result = await _sut.HandleAsync("Foo Fighters");

        Assert.Same(fromAggregator, result);
        _repo.Received(1).Add(Arg.Is<ArtistEntity>(e => e.Name == "Foo Fighters"));
    }

    [Fact]
    public async Task CacheMiss_AggregatorReturnsNull_DoesNotPersist_ReturnsNull()
    {
        _repo.GetByName("ghost").Returns((ArtistEntity?)null);
        _aggregator.GetArtistByNameAsync("ghost", Arg.Any<CancellationToken>())
                   .Returns((ArtistDto?)null);

        ArtistDto? result = await _sut.HandleAsync("ghost");

        Assert.Null(result);
        _repo.DidNotReceive().Add(Arg.Any<ArtistEntity>());
    }

    [Fact]
    public async Task CacheMiss_AcquiresKeyedLock_BeforeFetching()
    {
        _repo.GetByName("Foo Fighters").Returns((ArtistEntity?)null);
        _aggregator.GetArtistByNameAsync("Foo Fighters", Arg.Any<CancellationToken>())
                   .Returns(new ArtistDto { Name = "Foo Fighters", MusicBrainzID = "id" });

        await _sut.HandleAsync("Foo Fighters");

        await _locker.Received(1).LockAsync(
            Arg.Is<string>(k => k.StartsWith("artist:byname:")),
            Arg.Any<CancellationToken>());
    }


    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
