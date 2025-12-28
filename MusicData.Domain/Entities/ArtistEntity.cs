namespace MusicData.Domain.Entities;

public class ArtistEntity
{
    public int Id { get; set; }

    public DateTime UpdateDateTime { get; set; }

    public int Version { get; set; }

    public string Name { get; set; } = string.Empty;

    public string MusicBrainzID { get; set; } = string.Empty;

    public string? Biography { get; set; }

    public string? BiographyFR { get; set; }

    public string? Website { get; set; }

    public string? Wikipedia { get; set; }

    public string? Facebook { get; set; }

    public string? Twitter { get; set; }

    public string? Flickr { get; set; }

    public string? Instagram { get; set; }

    public string? AllMusic { get; set; }

    public string? LastFM { get; set; }

    public string? Discogs { get; set; }

    public string? Bandsintown { get; set; }

    public string? Youtube { get; set; }

    public string? FanartUrl { get; set; }

    public string? Fanart2Url { get; set; }

    public string? Fanart3Url { get; set; }

    public string? BannerUrl { get; set; }

    public string? LogoUrl { get; set; }

    public string? PictureUrl { get; set; }

    public string? CountryCode { get; set; }

    public string? AudioDbID { get; set; }

    public int? BeginYear { get; set; }

    public int? BornYear { get; set; }

    public int? EndYear { get; set; }

    public bool Disbanded { get; set; }

    public int FanartsCount { get; set; } = 0;

    public int PicturesCount { get; set; } = 0;

    public int LogosCount { get; set; } = 0;

    public int BannersCount { get; set; } = 0;

    public List<MemberEntity> Members { get; set; } = [];
}


public class MemberEntity
{
    public string Name { get; set; } = string.Empty;

    public string MusicBrainzID { get; set; } = string.Empty;
}