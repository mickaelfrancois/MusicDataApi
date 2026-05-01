using MusicData.Application.DTOs;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Tests.Application.Mappers;

public class ArtistMapperTests
{
    [Fact]
    public void RoundTrip_DtoToEntityToDto_PreservesAllPublicFields()
    {
        ArtistDto original = FullyPopulatedDto();

        ArtistEntity entity = original.ToEntity();
        ArtistDto roundTripped = entity.ToDto();

        // Origin is set externally (handler), not by mapper, so reset before compare.
        original.Origin = string.Empty;
        roundTripped.Origin = string.Empty;

        AssertArtistDtosEqual(original, roundTripped);
    }

    [Fact]
    public void EntityToDto_MapsWikipedia()
    {
        ArtistEntity entity = new() { Name = "x", MusicBrainzID = "id", Wikipedia = "https://wiki" };

        ArtistDto dto = entity.ToDto();

        Assert.Equal("https://wiki", dto.Wikipedia);
    }

    [Fact]
    public void DtoToEntity_PersistsAllSocialLinks()
    {
        // P0-4 regression guard: TikTok/Threads/SongKick/SoundCloud/Imdb were dropped
        // by the entity, so caching destroyed them silently.
        ArtistDto dto = new()
        {
            Name = "x",
            MusicBrainzID = "id",
            TikTok = "tt",
            Threads = "th",
            SongKick = "sk",
            SoundCloud = "sc",
            Imdb = "imdb",
            Fanart4Url = "f4",
            Fanart5Url = "f5",
        };

        ArtistEntity entity = dto.ToEntity();

        Assert.Equal("tt", entity.TikTok);
        Assert.Equal("th", entity.Threads);
        Assert.Equal("sk", entity.SongKick);
        Assert.Equal("sc", entity.SoundCloud);
        Assert.Equal("imdb", entity.Imdb);
        Assert.Equal("f4", entity.Fanart4Url);
        Assert.Equal("f5", entity.Fanart5Url);
    }

    [Fact]
    public void EntityToDto_MapsMembers()
    {
        ArtistEntity entity = new()
        {
            Name = "x",
            MusicBrainzID = "id",
            Members = [new MemberEntity { Name = "John", MusicBrainzID = "m1" }]
        };

        ArtistDto dto = entity.ToDto();

        Assert.Single(dto.Members);
        Assert.Equal("John", dto.Members[0].Name);
        Assert.Equal("m1", dto.Members[0].MusicBrainzID);
    }


    private static ArtistDto FullyPopulatedDto() => new()
    {
        Name = "Foo Fighters",
        MusicBrainzID = "67f66c07-6e61-4026-ade5-7e782fad3a5d",
        Biography = "American rock band",
        Website = "https://foofighters.com",
        Wikipedia = "https://en.wikipedia.org/wiki/Foo_Fighters",
        Facebook = "fb",
        Twitter = "tw",
        Flickr = "fl",
        Instagram = "ig",
        AllMusic = "am",
        TikTok = "tt",
        Threads = "th",
        SongKick = "sk",
        SoundCloud = "sc",
        Imdb = "imdb",
        LastFM = "lfm",
        Discogs = "dc",
        Bandsintown = "bit",
        Youtube = "yt",
        FanartUrl = "f1",
        Fanart2Url = "f2",
        Fanart3Url = "f3",
        Fanart4Url = "f4",
        Fanart5Url = "f5",
        BannerUrl = "banner",
        LogoUrl = "logo",
        PictureUrl = "pic",
        CountryCode = "US",
        AudioDbID = "adb",
        BeginYear = 1994,
        EndYear = null,
        Disbanded = false,
        Members =
        [
            new MemberDto { Name = "Dave Grohl", MusicBrainzID = "m1" },
            new MemberDto { Name = "Pat Smear", MusicBrainzID = "m2" }
        ]
    };

    private static void AssertArtistDtosEqual(ArtistDto a, ArtistDto b)
    {
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.MusicBrainzID, b.MusicBrainzID);
        Assert.Equal(a.Biography, b.Biography);
        Assert.Equal(a.Website, b.Website);
        Assert.Equal(a.Wikipedia, b.Wikipedia);
        Assert.Equal(a.Facebook, b.Facebook);
        Assert.Equal(a.Twitter, b.Twitter);
        Assert.Equal(a.Flickr, b.Flickr);
        Assert.Equal(a.Instagram, b.Instagram);
        Assert.Equal(a.AllMusic, b.AllMusic);
        Assert.Equal(a.TikTok, b.TikTok);
        Assert.Equal(a.Threads, b.Threads);
        Assert.Equal(a.SongKick, b.SongKick);
        Assert.Equal(a.SoundCloud, b.SoundCloud);
        Assert.Equal(a.Imdb, b.Imdb);
        Assert.Equal(a.LastFM, b.LastFM);
        Assert.Equal(a.Discogs, b.Discogs);
        Assert.Equal(a.Bandsintown, b.Bandsintown);
        Assert.Equal(a.Youtube, b.Youtube);
        Assert.Equal(a.FanartUrl, b.FanartUrl);
        Assert.Equal(a.Fanart2Url, b.Fanart2Url);
        Assert.Equal(a.Fanart3Url, b.Fanart3Url);
        Assert.Equal(a.Fanart4Url, b.Fanart4Url);
        Assert.Equal(a.Fanart5Url, b.Fanart5Url);
        Assert.Equal(a.BannerUrl, b.BannerUrl);
        Assert.Equal(a.LogoUrl, b.LogoUrl);
        Assert.Equal(a.PictureUrl, b.PictureUrl);
        Assert.Equal(a.CountryCode, b.CountryCode);
        Assert.Equal(a.AudioDbID, b.AudioDbID);
        Assert.Equal(a.BeginYear, b.BeginYear);
        Assert.Equal(a.EndYear, b.EndYear);
        Assert.Equal(a.Disbanded, b.Disbanded);
        Assert.Equal(a.Members.Count, b.Members.Count);
        for (int i = 0; i < a.Members.Count; i++)
        {
            Assert.Equal(a.Members[i].Name, b.Members[i].Name);
            Assert.Equal(a.Members[i].MusicBrainzID, b.Members[i].MusicBrainzID);
        }
    }
}
