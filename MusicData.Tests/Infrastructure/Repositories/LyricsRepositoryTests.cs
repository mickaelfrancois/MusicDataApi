using LiteDB;
using MusicData.Domain.Entities;
using MusicData.Infrastructure.Repositories;

namespace MusicData.Tests.Infrastructure.Repositories;

public class LyricsRepositoryTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");
    private readonly LyricsRepository _sut;

    public LyricsRepositoryTests()
    {
        _sut = new LyricsRepository(_db);
        LyricsRepository.EnsureIndexes(_db.GetCollection<LyricsEntity>(LyricsRepository.CollectionName));
    }

    public void Dispose() => _db.Dispose();


    [Fact]
    public void Get_DifferentCase_MatchesCachedRow()
    {
        _sut.Add(new LyricsEntity
        {
            Title = "Walk",
            ArtistName = "Foo Fighters",
            PlainLyrics = "lyrics"
        });

        Assert.NotNull(_sut.Get("walk", "foo fighters"));
        Assert.NotNull(_sut.Get("WALK", "FOO FIGHTERS"));
    }

    [Fact]
    public void Get_DifferentArtist_DoesNotMatch()
    {
        _sut.Add(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters" });

        Assert.Null(_sut.Get("Walk", "Pantera"));
    }

    [Fact]
    public void Add_SameTitleAndArtist_Upserts()
    {
        _sut.Add(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters", PlainLyrics = "v1" });
        _sut.Add(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters", PlainLyrics = "v2" });

        LyricsEntity? loaded = _sut.Get("Walk", "Foo Fighters");
        Assert.NotNull(loaded);
        Assert.Equal("v2", loaded!.PlainLyrics);
    }

    [Fact]
    public void Get_LegacyVersionRow_IsTreatedAsStaleAndReturnsNull()
    {
        // Pre-P2-2 cache rows had Version = 1 and lacked AlbumName / Duration.
        ILiteCollection<LyricsEntity> raw = _db.GetCollection<LyricsEntity>("lyrics");
        raw.Insert(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters", Version = 1 });

        Assert.Null(_sut.Get("Walk", "Foo Fighters"));
    }

    [Fact]
    public void Add_PreservesAlbumNameAndDuration()
    {
        _sut.Add(new LyricsEntity
        {
            Title = "Walk",
            ArtistName = "Foo Fighters",
            AlbumName = "Wasting Light",
            Duration = 257
        });

        LyricsEntity? loaded = _sut.Get("Walk", "Foo Fighters");
        Assert.NotNull(loaded);
        Assert.Equal("Wasting Light", loaded!.AlbumName);
        Assert.Equal(257, loaded.Duration);
    }

    [Fact]
    public void Get_PositiveRow_ExpiresAfter30Days_NegativeMarkerAfter1Day()
    {
        _sut.Add(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters", PlainLyrics = "lyrics" });
        _sut.Add(new LyricsEntity { Title = "Nope", ArtistName = "Nobody", NotFound = true });

        Age("Walk", TimeSpan.FromDays(2));
        Age("Nope", TimeSpan.FromDays(2));

        Assert.NotNull(_sut.Get("Walk", "Foo Fighters"));
        Assert.Null(_sut.Get("Nope", "Nobody"));

        Age("Walk", TimeSpan.FromDays(31));

        Assert.Null(_sut.Get("Walk", "Foo Fighters"));
    }

    [Fact]
    public void Get_PartialLyrics_ExpireAfterOneHour()
    {
        _sut.Add(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters", PlainLyrics = "lyrics", IsPartial = true });

        Assert.NotNull(_sut.Get("Walk", "Foo Fighters"));

        Age("Walk", TimeSpan.FromHours(2));

        Assert.Null(_sut.Get("Walk", "Foo Fighters"));
    }

    [Fact]
    public void Add_OverExpiredRow_RefreshesItInPlace()
    {
        _sut.Add(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters", NotFound = true });
        Age("Walk", TimeSpan.FromDays(2));

        _sut.Add(new LyricsEntity { Title = "Walk", ArtistName = "Foo Fighters", PlainLyrics = "found" });

        Assert.Equal("found", _sut.Get("Walk", "Foo Fighters")!.PlainLyrics);
        Assert.Equal(1, _db.GetCollection<LyricsEntity>(LyricsRepository.CollectionName).Count());
    }


    private void Age(string title, TimeSpan by)
    {
        ILiteCollection<LyricsEntity> col = _db.GetCollection<LyricsEntity>(LyricsRepository.CollectionName);
        LyricsEntity row = col.FindOne(x => x.Title == title);
        row.UpdateDateTime = DateTime.UtcNow - by;
        col.Update(row);
    }
}
