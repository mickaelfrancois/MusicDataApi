using MusicData.Application.DTOs;
using MusicData.Domain.Entities;

namespace MusicData.Application.Mappers;

public static class TrackMapper
{
    public static TrackDto ToDto(this Track entity) => Map(entity);

    public static Track ToEntity(this TrackDto dto) => Map(dto);

    public static TrackDto Map(Track entity)
    {
        TrackDto dto = new()
        {
            Name = entity.Name,
            Position = entity.Position,
            Duration = entity.Duration
        };

        return dto;
    }

    public static Track Map(TrackDto dto)
    {
        Track entity = new()
        {
            Name = dto.Name,
            Position = dto.Position,
            Duration = dto.Duration
        };

        return entity;
    }
}