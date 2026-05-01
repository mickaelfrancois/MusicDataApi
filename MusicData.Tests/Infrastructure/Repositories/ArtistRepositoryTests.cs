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
        ArtistRepository.EnsureIndexes(_db.GetCollection<ArtistEntity>(ArtistRepository.CollectionName));
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

    [Fact]
    public void Get_LegacyVersionRow_IsTreatedAsStaleAndReturnsNull()
    {
        // Insert a row that simulates a pre-P0-4 cache entry: schema version 1.
        ILiteCollection<ArtistEntity> raw = _db.GetCollection<ArtistEntity>("artists");
        raw.Insert(new ArtistEntity { Name = "Old Crow", MusicBrainzID = "legacy-mbid", Version = 1 });

        Assert.Null(_sut.GetByMusicBrainzID("legacy-mbid"));
        Assert.Null(_sut.GetByName("Old Crow"));
    }

    [Fact]
    public void Add_StampsCurrentSchemaVersion()
    {
        _sut.Add(new ArtistEntity { Name = "Fresh", MusicBrainzID = "fresh-mbid" });

        ILiteCollection<ArtistEntity> raw = _db.GetCollection<ArtistEntity>("artists");
        ArtistEntity? stored = raw.FindOne(x => x.MusicBrainzID == "fresh-mbid");

        Assert.NotNull(stored);
        Assert.Equal(2, stored!.Version);
    }

    [Fact]
    public void Add_OverLegacyRow_UpgradesItToCurrentSchemaVersion()
    {
        ILiteCollection<ArtistEntity> raw = _db.GetCollection<ArtistEntity>("artists");
        raw.Insert(new ArtistEntity { Name = "Upgrade Me", MusicBrainzID = "u-mbid", Version = 1 });

        _sut.Add(new ArtistEntity { Name = "Upgrade Me", MusicBrainzID = "u-mbid", Biography = "refreshed" });

        Assert.NotNull(_sut.GetByMusicBrainzID("u-mbid"));
        Assert.Equal("refreshed", _sut.GetByMusicBrainzID("u-mbid")!.Biography);
    }
}
