using LiteDB;
using MusicData.Domain.Entities;
using MusicData.Infrastructure.Repositories;

namespace MusicData.Tests.Infrastructure.Repositories;

public class AlbumRepositoryTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");
    private readonly AlbumRepository _sut;

    public AlbumRepositoryTests()
    {
        _sut = new AlbumRepository(_db);
        AlbumRepository.EnsureIndexes(_db.GetCollection<AlbumEntity>(AlbumRepository.CollectionName));
    }

    public void Dispose() => _db.Dispose();


    [Fact]
    public void GetByName_DifferentCase_MatchesCachedRow()
    {
        _sut.Add(new AlbumEntity
        {
            Name = "Wasting Light",
            Artist = "Foo Fighters",
            MusicBrainzID = "mbid-1",
            MusicBrainzArtistID = "art-1"
        });

        Assert.NotNull(_sut.GetByName("wasting light", "foo fighters"));
        Assert.NotNull(_sut.GetByName("WASTING LIGHT", "FOO FIGHTERS"));
    }

    [Fact]
    public void GetByName_RequiresBothNameAndArtistToMatch()
    {
        _sut.Add(new AlbumEntity
        {
            Name = "Greatest Hits",
            Artist = "Foo Fighters",
            MusicBrainzID = "mbid-1",
            MusicBrainzArtistID = "art-1"
        });
        _sut.Add(new AlbumEntity
        {
            Name = "Greatest Hits",
            Artist = "Queen",
            MusicBrainzID = "mbid-2",
            MusicBrainzArtistID = "art-2"
        });

        AlbumEntity? foo = _sut.GetByName("Greatest Hits", "Foo Fighters");
        AlbumEntity? queen = _sut.GetByName("Greatest Hits", "Queen");

        Assert.NotNull(foo);
        Assert.NotNull(queen);
        Assert.Equal("mbid-1", foo!.MusicBrainzID);
        Assert.Equal("mbid-2", queen!.MusicBrainzID);
    }

    [Fact]
    public void GetByMusicBrainzID_DifferentCase_MatchesCachedRow()
    {
        _sut.Add(new AlbumEntity
        {
            Name = "X",
            MusicBrainzID = "ABC-123",
            MusicBrainzArtistID = "art-1"
        });

        Assert.NotNull(_sut.GetByMusicBrainzID("abc-123"));
    }

    [Fact]
    public void Add_SameMusicBrainzId_Upserts()
    {
        _sut.Add(new AlbumEntity { Name = "X", MusicBrainzID = "mbid-1", Biography = "old" });
        _sut.Add(new AlbumEntity { Name = "X", MusicBrainzID = "mbid-1", Biography = "new" });

        Assert.Equal("new", _sut.GetByMusicBrainzID("mbid-1")!.Biography);
    }
}
