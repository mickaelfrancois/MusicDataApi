using MusicData.Application.DTOs;

namespace MusicData.Infrastructure.Services.Fanart;


internal static class FanartMapper
{
    public static ArtistDto MapArtist(FanartRoot root)
    {
        List<FanartImage> backgrounds = root.ArtistBackground?
            .OrderByDescending(c => c.Score)
            .ToList() ?? [];

        string pictureUrl = root.ArtistThumb?
            .OrderByDescending(c => c.Score)
            .FirstOrDefault()?.Url ?? string.Empty;

        string bannerUrl = root.MusicBanner?
            .OrderByDescending(c => c.Score)
            .FirstOrDefault()?.Url ?? string.Empty;

        string logoUrl = root.MusicLogo?
            .OrderByDescending(c => c.Score)
            .FirstOrDefault()?.Url ?? string.Empty;

        string fanart1 = backgrounds.ElementAtOrDefault(0)?.Url ?? string.Empty;
        string fanart2 = backgrounds.ElementAtOrDefault(1)?.Url ?? string.Empty;
        string fanart3 = backgrounds.ElementAtOrDefault(2)?.Url ?? string.Empty;
        string fanart4 = backgrounds.ElementAtOrDefault(3)?.Url ?? string.Empty;
        string fanart5 = backgrounds.ElementAtOrDefault(4)?.Url ?? string.Empty;

        return new ArtistDto()
        {
            PictureUrl = pictureUrl,
            FanartUrl = fanart1,
            Fanart2Url = fanart2,
            Fanart3Url = fanart3,
            Fanart4Url = fanart4,
            Fanart5Url = fanart5,
            BannerUrl = bannerUrl,
            LogoUrl = logoUrl
        };
    }

    public static AlbumDto MapAlbum(FanartRoot root)
    {
        string coverUrl = root.ArtistThumb?
            .OrderByDescending(c => c.Score)
            .FirstOrDefault()?.Url ?? string.Empty;

        return new AlbumDto()
        {
            PictureUrl = coverUrl
        };
    }
}