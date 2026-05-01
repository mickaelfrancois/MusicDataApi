using MusicData.Application.DTOs;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Tests.Application.Mappers;

public class AlbumMapperTests
{
    [Fact]
    public void RoundTrip_DtoToEntityToDto_PreservesAllPublicFields()
    {
        AlbumDto original = FullyPopulatedDto();

        AlbumEntity entity = original.ToEntity();
        AlbumDto roundTripped = entity.ToDto();

        original.Origin = string.Empty;
        roundTripped.Origin = string.Empty;

        AssertAlbumDtosEqual(original, roundTripped);
    }

    [Fact]
    public void RoundTrip_PreservesTracks()
    {
        AlbumDto original = new()
        {
            Name = "Wasting Light",
            MusicBrainzID = "id",
            Tracks =
            {
                new TrackDto { Position = 1, Name = "Bridge Burning", Duration = 285 },
                new TrackDto { Position = 2, Name = "Rope", Duration = 257 }
            }
        };

        AlbumDto roundTripped = original.ToEntity().ToDto();

        Assert.Equal(2, roundTripped.Tracks.Count);
        Assert.Equal("Bridge Burning", roundTripped.Tracks[0].Name);
        Assert.Equal(1, roundTripped.Tracks[0].Position);
        Assert.Equal(285, roundTripped.Tracks[0].Duration);
    }

    private static AlbumDto FullyPopulatedDto() => new()
    {
        Name = "Wasting Light",
        MusicBrainzID = "98cfdc56-7acf-4ecb-8b42-71c5b1c7421b",
        MusicBrainzArtistID = "67f66c07-6e61-4026-ade5-7e782fad3a5d",
        Artist = "Foo Fighters",
        Biography = "studio album",
        Wikipedia = "https://en.wikipedia.org/wiki/Wasting_Light",
        PictureUrl = "pic",
        LastFM = "lfm",
        Year = "2011",
        ReleaseGroupMusicBrainzID = "rg",
        AudioDbID = "adb",
        AudioDbArtistID = "adb-artist",
        ReleaseFormat = "CD",
        Sales = "1M",
        AllMusicID = "am",
        DiscogsID = "dc",
        MusicMozID = "mm",
        LyricWikiID = "lw",
        GeniusID = "gn",
        WikipediaID = "wpid",
        WikidataID = "wdid",
        AmazonID = "azn",
        Score = 95,
        Label = "RCA",
        Genre = "Rock",
        ReleaseDate = new DateTime(2011, 4, 12),
    };

    private static void AssertAlbumDtosEqual(AlbumDto a, AlbumDto b)
    {
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.MusicBrainzID, b.MusicBrainzID);
        Assert.Equal(a.MusicBrainzArtistID, b.MusicBrainzArtistID);
        Assert.Equal(a.Artist, b.Artist);
        Assert.Equal(a.Biography, b.Biography);
        Assert.Equal(a.Wikipedia, b.Wikipedia);
        Assert.Equal(a.PictureUrl, b.PictureUrl);
        Assert.Equal(a.LastFM, b.LastFM);
        Assert.Equal(a.Year, b.Year);
        Assert.Equal(a.ReleaseGroupMusicBrainzID, b.ReleaseGroupMusicBrainzID);
        Assert.Equal(a.AudioDbID, b.AudioDbID);
        Assert.Equal(a.AudioDbArtistID, b.AudioDbArtistID);
        Assert.Equal(a.ReleaseFormat, b.ReleaseFormat);
        Assert.Equal(a.Sales, b.Sales);
        Assert.Equal(a.AllMusicID, b.AllMusicID);
        Assert.Equal(a.DiscogsID, b.DiscogsID);
        Assert.Equal(a.MusicMozID, b.MusicMozID);
        Assert.Equal(a.LyricWikiID, b.LyricWikiID);
        Assert.Equal(a.GeniusID, b.GeniusID);
        Assert.Equal(a.WikipediaID, b.WikipediaID);
        Assert.Equal(a.WikidataID, b.WikidataID);
        Assert.Equal(a.AmazonID, b.AmazonID);
        Assert.Equal(a.Score, b.Score);
        Assert.Equal(a.Label, b.Label);
        Assert.Equal(a.Genre, b.Genre);
        Assert.Equal(a.ReleaseDate, b.ReleaseDate);
    }
}
