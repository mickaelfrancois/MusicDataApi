using Hqub.MusicBrainz.Entities;
using MusicData.Application.DTOs;

namespace MusicData.Infrastructure.Services.MusicBrainz;

internal static class MusicBrainzMapper
{
    public static ArtistDto? Map(Artist? artist)
    {
        if (artist is null)
            return null;

        ArtistDto dto = new()
        {
            Name = artist.Name ?? string.Empty,
            MusicBrainzID = artist.Id ?? null,
            CountryCode = artist.Country ?? null,
            Biography = null,
            PictureUrl = null
        };

        if (artist.LifeSpan?.Begin is not null && int.TryParse(GetYear(artist.LifeSpan.Begin), out int formed))
            dto.BeginYear = formed;

        if (artist.LifeSpan?.End is not null && int.TryParse(GetYear(artist.LifeSpan.End), out int ended))
            dto.EndYear = ended;

        dto.Disbanded = artist.LifeSpan?.Ended == true;

        dto.Members = MapMembers(artist);
        dto = MapSocialNetworks(artist, dto);

        return dto;
    }

    public static AlbumDto? Map(Release? release)
    {
        if (release is null)
            return null;

        AlbumDto dto = new()
        {
            Name = release.Title ?? string.Empty,
            MusicBrainzID = release.Id ?? null,
            ReleaseGroupMusicBrainzID = release.ReleaseGroup?.Id ?? null,
            ReleaseFormat = release.Media != null && release.Media.Count > 0 ? release.Media[0].Format : null,
            Label = release.Labels != null && release.Labels.Count > 0 ? release.Labels[0].Label?.Name : null,
            Genre = release.Genres != null && release.Genres.Count > 0 ? release.Genres[0].Name : null,
            AllMusicID = release.Relations?.FirstOrDefault(c => c.Type == "allmusic")?.Url?.Resource ?? null,
            AmazonID = release.Relations?.FirstOrDefault(c => c.Type == "amazon")?.Url?.Resource ?? null,
            DiscogsID = release.Relations?.FirstOrDefault(c => c.Type == "discogs")?.Url?.Resource ?? null,
            GeniusID = release.Relations?.FirstOrDefault(c => c.Type == "genius")?.Url?.Resource ?? null,
            WikipediaID = release.Relations?.FirstOrDefault(c => c.Type == "wikipedia")?.Url?.Resource ?? null,
            WikidataID = release.Relations?.FirstOrDefault(c => c.Type == "wikidata")?.Url?.Resource ?? null,
            LastFM = release.Relations?.FirstOrDefault(c => c.Type == "last.fm")?.Url?.Resource ?? null,
            Year = release.Date != null && release.Date.Length >= 4 ? release.Date.Substring(0, 4) : null,
            PictureUrl = null,
            Score = release.Score,
            Sales = null,
            Biography = null
        };

        if (DateTime.TryParse(release.ReleaseGroup?.FirstReleaseDate, out DateTime firstReleaseDate))
            dto.ReleaseDate = firstReleaseDate;
        else if (DateTime.TryParse(release.Date, out DateTime releaseDate))
            dto.ReleaseDate = releaseDate;

        return dto;
    }

    private static List<MemberDto> MapMembers(Artist artist)
    {
        if (artist.Relations is null)
            return new List<MemberDto>();

        return artist.Relations
            .Where(r => r.TargetType == "artist" && r.Type.Contains("member") && !r.Ended.GetValueOrDefault())
            .Select(relation => new MemberDto
            {
                Name = relation.Artist?.Name ?? string.Empty,
                MusicBrainzID = relation.Artist?.Id ?? string.Empty
            })
            .ToList();
    }

    private static ArtistDto MapSocialNetworks(Artist artist, ArtistDto dto)
    {
        if (artist.Relations is null)
            return dto;

        dto.Website = artist.Relations.FirstOrDefault(c => c.Type == "official homepage")?.Url?.Resource ?? null;
        dto.Bandsintown = artist.Relations.FirstOrDefault(c => c.Type == "bandsintown")?.Url?.Resource ?? null;
        dto.Discogs = artist.Relations.FirstOrDefault(c => c.Type == "discogs")?.Url?.Resource ?? null;
        dto.Imdb = artist.Relations.FirstOrDefault(c => c.Type == "IMDb")?.Url?.Resource ?? null;
        dto.LastFM = artist.Relations.FirstOrDefault(c => c.Type == "last.fm")?.Url?.Resource ?? null;
        dto.SongKick = artist.Relations.FirstOrDefault(c => c.Type == "songkick")?.Url?.Resource ?? null;
        dto.SoundCloud = artist.Relations.FirstOrDefault(c => c.Type == "soundcloud")?.Url?.Resource ?? null;
        dto.Youtube = artist.Relations.FirstOrDefault(c => c.Type == "youtube")?.Url?.Resource ?? null;
        dto.AllMusic = artist.Relations.FirstOrDefault(c => c.Type == "allmusic")?.Url?.Resource ?? null;

        foreach (Relation? relation in artist.Relations.Where(c => c.Type == "social network"))
        {
            string? url = relation?.Url?.Resource ?? null;
            if (string.IsNullOrWhiteSpace(url))
                continue;

            string lower = url.ToLowerInvariant();
            if (lower.Contains("wikipedia.org")) { dto.Wikipedia = url; continue; }
            if (lower.Contains("facebook.com")) { dto.Facebook = url; continue; }
            if (lower.Contains("twitter.com")) { dto.Twitter = url; continue; }
            if (lower.Contains("instagram.com")) { dto.Instagram = url; continue; }
            if (lower.Contains("tiktok.com")) { dto.TikTok = url; continue; }
            if (lower.Contains("threads.com")) { dto.Threads = url; }
        }

        return dto;
    }

    private static string GetYear(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Length >= 4 ? value.Substring(0, 4) : value;
    }
}