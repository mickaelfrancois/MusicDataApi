using MusicData.Application.DTOs;
using MusicData.Domain.Entities;

namespace MusicData.Application.Mappers;

public static class LyricsMapper
{
    public static LyricsDto ToDto(this LyricsEntity entity) => Map(entity);

    public static LyricsEntity ToEntity(this LyricsDto dto) => Map(dto);

    public static LyricsDto Map(LyricsEntity entity)
    {
        if (entity is null) return null!;

        LyricsDto dto = new()
        {
            ArtistName = entity.ArtistName,
            Title = entity.Title,
            AlbumName = entity.AlbumName,
            Duration = entity.Duration,
            PlainLyrics = entity.PlainLyrics,
            SyncLyrics = entity.SyncLyrics
        };

        return dto;
    }


    public static LyricsEntity Map(LyricsDto dto)
    {
        LyricsEntity entity = new()
        {
            ArtistName = dto.ArtistName,
            Title = dto.Title,
            AlbumName = dto.AlbumName,
            Duration = dto.Duration,
            PlainLyrics = dto.PlainLyrics,
            SyncLyrics = dto.SyncLyrics
        };

        return entity;
    }
}
