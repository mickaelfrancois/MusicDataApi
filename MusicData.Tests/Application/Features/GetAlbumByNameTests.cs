using Microsoft.Extensions.Logging.Abstractions;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Albums;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;
using NSubstitute;

namespace MusicData.Tests.Application.Features;

public class GetAlbumByNameTests
{
    private readonly IAlbumRepository _repo = Substitute.For<IAlbumRepository>();
    private readonly IMusicAggregator _aggregator = Substitute.For<IMusicAggregator>();
    private readonly IKeyedLocker _locker = Substitute.For<IKeyedLocker>();
    private readonly GetAlbumByName _sut;

    public GetAlbumByNameTests()
    {
        _locker.LockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromResult<IDisposable>(Substitute.For<IDisposable>()));

        _sut = new GetAlbumByName(_repo, _aggregator, _locker, NullLogger<GetAlbumByName>.Instance);
    }

    [Fact]
    public async Task CacheMiss_ResolvedUnderAnotherTitle_PersistsTheQueriedNameAsAlias()
    {
        _aggregator.GetAlbumByNameAsync("Abbey Road (Remastered)", "art-1", Arg.Any<CancellationToken>())
                   .Returns(new AlbumDto { Name = "Abbey Road", MusicBrainzID = "alb-1", MusicBrainzArtistID = "art-1" });

        await _sut.HandleAsync("Abbey Road (Remastered)", "art-1");

        _repo.Received(1).Add(Arg.Is<AlbumEntity>(e =>
            e.Name == "Abbey Road" && e.Aliases.SequenceEqual(new[] { "abbey road (remastered)" })));
    }

    [Fact]
    public async Task CacheMiss_ResolvedUnderTheSameTitle_PersistsNoAlias()
    {
        _aggregator.GetAlbumByNameAsync("abbey road", "art-1", Arg.Any<CancellationToken>())
                   .Returns(new AlbumDto { Name = "Abbey Road", MusicBrainzID = "alb-1", MusicBrainzArtistID = "art-1" });

        await _sut.HandleAsync("abbey road", "art-1");

        _repo.Received(1).Add(Arg.Is<AlbumEntity>(e => e.Aliases.Count == 0));
    }
}
