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

        Assert.NotNull(_sut.GetByName("wasting light", "ART-1"));
        Assert.NotNull(_sut.GetByName("WASTING LIGHT", "art-1"));
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

        AlbumEntity? foo = _sut.GetByName("Greatest Hits", "art-1");
        AlbumEntity? queen = _sut.GetByName("Greatest Hits", "art-2");

        Assert.NotNull(foo);
        Assert.NotNull(queen);
        Assert.Equal("mbid-1", foo!.MusicBrainzID);
        Assert.Equal("mbid-2", queen!.MusicBrainzID);
    }

    [Fact]
    public void GetByName_ArtistName_DoesNotMatch_OnlyArtistMbidDoes()
    {
        // Regression: the by-name handler passes the artist MBID, which used
        // to be compared against the artist *name* column and never matched.
        _sut.Add(new AlbumEntity
        {
            Name = "Wasting Light",
            Artist = "Foo Fighters",
            MusicBrainzID = "mbid-1",
            MusicBrainzArtistID = "art-1"
        });

        Assert.Null(_sut.GetByName("Wasting Light", "Foo Fighters"));
        Assert.NotNull(_sut.GetByName("Wasting Light", "art-1"));
    }

    [Fact]
    public void GetByName_Alias_MatchesCachedRow_ForTheSameArtistOnly()
    {
        _sut.Add(new AlbumEntity
        {
            Name = "Abbey Road",
            MusicBrainzID = "mbid-1",
            MusicBrainzArtistID = "art-1",
            Aliases = ["abbey road (remastered)"]
        });

        Assert.Equal("mbid-1", _sut.GetByName("Abbey Road (Remastered)", "ART-1")?.MusicBrainzID);
        Assert.Null(_sut.GetByName("Abbey Road (Remastered)", "art-2"));
    }

    [Fact]
    public void Add_OverExistingRow_KeepsItsAliases()
    {
        _sut.Add(new AlbumEntity { Name = "Abbey Road", MusicBrainzID = "mbid-1", MusicBrainzArtistID = "art-1", Aliases = ["abbey road (remastered)"] });
        _sut.Add(new AlbumEntity { Name = "Abbey Road", MusicBrainzID = "mbid-1", MusicBrainzArtistID = "art-1" });

        Assert.NotNull(_sut.GetByName("Abbey Road (Remastered)", "art-1"));
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
