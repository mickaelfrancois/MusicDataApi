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
}
