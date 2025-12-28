using MusicData.Application.DTOs;

namespace MusicData.Infrastructure.Services.CoverArt;

public static class CoverArtMapper
{
    public static AlbumDto Map(CoverArtRootObject album)
    {
        return new AlbumDto()
        {
            PictureUrl = album.Images?.FirstOrDefault(c => c.Front && c.Approved)?.Image
        };
    }
}
