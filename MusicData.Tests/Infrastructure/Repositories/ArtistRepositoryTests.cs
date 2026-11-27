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
    public void GetByName_Alias_MatchesCachedRow()
    {
        _sut.Add(new ArtistEntity { Name = "The Beatles", MusicBrainzID = "mbid-1", Aliases = ["beatles"] });

        Assert.Equal("mbid-1", _sut.GetByName("Beatles")?.MusicBrainzID);
        Assert.Equal("mbid-1", _sut.GetByName("the beatles")?.MusicBrainzID);
        Assert.Null(_sut.GetByName("beatle"));
    }

    [Fact]
    public void Add_OverExistingRow_KeepsItsAliases()
    {
        _sut.Add(new ArtistEntity { Name = "The Beatles", MusicBrainzID = "mbid-1", Aliases = ["beatles"] });

        // Refreshed through another route (by MBID, or another alias).
        _sut.Add(new ArtistEntity { Name = "The Beatles", MusicBrainzID = "mbid-1", Biography = "new" });
        _sut.Add(new ArtistEntity { Name = "The Beatles", MusicBrainzID = "mbid-1", Aliases = ["fab four"] });

        Assert.NotNull(_sut.GetByName("Beatles"));
        Assert.NotNull(_sut.GetByName("Fab Four"));
        Assert.Equal(1, _db.GetCollection<ArtistEntity>("artists").Count());
    }

    [Fact]
    public void Add_ManyDistinctAliases_KeepsOnlyTheMostRecentTwenty()
    {
        for (int i = 0; i < 200; i++)
            _sut.Add(new ArtistEntity { Name = "The Beatles", MusicBrainzID = "mbid-1", Aliases = [$"variant {i}"] });

        ArtistEntity stored = _db.GetCollection<ArtistEntity>("artists").FindAll().Single();
        Assert.Equal(20, stored.Aliases.Count);
        Assert.Equal(Enumerable.Range(180, 20).Select(i => $"variant {i}"), stored.Aliases);
        Assert.NotNull(_sut.GetByName("Variant 199"));
        Assert.Null(_sut.GetByName("variant 0"));
    }

    [Fact]
    public void Add_TrivialAliasVariants_CollapseIntoOne_AndStillMatch()
    {
        _sut.Add(new ArtistEntity { Name = "The Beatles", MusicBrainzID = "mbid-1", Aliases = ["fab four"] });
        _sut.Add(new ArtistEntity { Name = "The Beatles", MusicBrainzID = "mbid-1", Aliases = ["  Fab   FOUR "] });

        Assert.Equal(["fab four"], _db.GetCollection<ArtistEntity>("artists").FindAll().Single().Aliases);
        Assert.NotNull(_sut.GetByName(" FAB  four"));
    }

    [Fact]
    public void GetByName_StaleHomonym_DoesNotHideAFreshRow()
    {
        ILiteCollection<ArtistEntity> raw = _db.GetCollection<ArtistEntity>("artists");
        raw.Insert(new ArtistEntity { Name = "Genesis", MusicBrainzID = "mbid-stale", Version = 2, UpdateDateTime = DateTime.UtcNow.AddDays(-60) });
        _sut.Add(new ArtistEntity { Name = "Genesis", MusicBrainzID = "mbid-fresh" });

        Assert.Equal("mbid-fresh", _sut.GetByName("Genesis")?.MusicBrainzID);
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
    public void Get_PartialRow_ExpiresAfterOneHour_CompleteRowDoesNot()
    {
        _sut.Add(new ArtistEntity { Name = "Partial", MusicBrainzID = "p-mbid", IsPartial = true });
        _sut.Add(new ArtistEntity { Name = "Complete", MusicBrainzID = "c-mbid" });

        Assert.NotNull(_sut.GetByMusicBrainzID("p-mbid"));

        ILiteCollection<ArtistEntity> raw = _db.GetCollection<ArtistEntity>("artists");
        foreach (ArtistEntity row in raw.FindAll().ToList())
        {
            row.UpdateDateTime = DateTime.UtcNow - TimeSpan.FromHours(2);
            raw.Update(row);
        }

        Assert.Null(_sut.GetByMusicBrainzID("p-mbid"));
        Assert.Null(_sut.GetByName("Partial"));
        Assert.NotNull(_sut.GetByMusicBrainzID("c-mbid"));
    }

    [Fact]
    public void Add_CompleteResult_OverPartialRow_ClearsTheFlag()
    {
        _sut.Add(new ArtistEntity { Name = "Foo", MusicBrainzID = "mbid-1", IsPartial = true });
        _sut.Add(new ArtistEntity { Name = "Foo", MusicBrainzID = "mbid-1" });

        Assert.False(_sut.GetByMusicBrainzID("mbid-1")!.IsPartial);
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
