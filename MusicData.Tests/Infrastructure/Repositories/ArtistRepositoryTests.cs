using LiteDB;
using MusicData.Domain.Entities;
using MusicData.Infrastructure.Repositories;

namespace MusicData.Tests.Infrastructure.Repositories;

public class ArtistRepositoryTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");
    private readonly ArtistRepository _sut;

    public ArtistRepositoryTests()
    {
        _sut = new ArtistRepository(_db);
    }

    public void Dispose() => _db.Dispose();


    [Fact]
    public void Add_NewArtist_InsertsRow()
    {
        ArtistEntity artist = new() { Name = "Foo Fighters", MusicBrainzID = "mbid-1" };

        _sut.Add(artist);

        ArtistEntity? loaded = _sut.GetByMusicBrainzID("mbid-1");
        Assert.NotNull(loaded);
        Assert.Equal("Foo Fighters", loaded!.Name);
    }

    [Fact]
    public void Add_SameMusicBrainzId_Upserts()
    {
        _sut.Add(new ArtistEntity { Name = "Foo Fighters", MusicBrainzID = "mbid-1", Biography = "old" });
        _sut.Add(new ArtistEntity { Name = "Foo Fighters", MusicBrainzID = "mbid-1", Biography = "new" });

        ArtistEntity? loaded = _sut.GetByMusicBrainzID("mbid-1");
        Assert.NotNull(loaded);
        Assert.Equal("new", loaded!.Biography);
    }

    [Fact]
    public void Add_TwoDifferentArtistsWithSameName_BothPersist()
    {
        // P0-5 regression guard: a unique index on Name would have thrown here.
        _sut.Add(new ArtistEntity { Name = "Genesis", MusicBrainzID = "mbid-rock" });
        _sut.Add(new ArtistEntity { Name = "Genesis", MusicBrainzID = "mbid-other" });

        Assert.NotNull(_sut.GetByMusicBrainzID("mbid-rock"));
        Assert.NotNull(_sut.GetByMusicBrainzID("mbid-other"));
    }

    [Fact]
    public void GetByName_DifferentCase_MatchesCachedRow()
    {
        // P0-6 regression guard: LiteDB ignored StringComparison and returned null.
        _sut.Add(new ArtistEntity { Name = "Foo Fighters", MusicBrainzID = "mbid-1" });

        Assert.NotNull(_sut.GetByName("foo fighters"));
        Assert.NotNull(_sut.GetByName("FOO FIGHTERS"));
        Assert.NotNull(_sut.GetByName("Foo Fighters"));
    }

    [Fact]
    public void GetByMusicBrainzID_DifferentCase_MatchesCachedRow()
    {
        _sut.Add(new ArtistEntity { Name = "Foo Fighters", MusicBrainzID = "ABC-123" });

        Assert.NotNull(_sut.GetByMusicBrainzID("abc-123"));
        Assert.NotNull(_sut.GetByMusicBrainzID("ABC-123"));
    }

    [Fact]
    public void GetByName_UnknownName_ReturnsNull()
    {
        Assert.Null(_sut.GetByName("nobody"));
    }
}
