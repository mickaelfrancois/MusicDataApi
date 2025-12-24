using MusicData.Application.DTOs;
using MusicData.Domain.Entities;

namespace MusicData.Application.Mappers;

public static class MemberMapper
{
    public static MemberDto? ToDto(this MemberEntity? entity) => Map(entity);

    public static MemberEntity? ToEntity(this MemberDto? dto) => Map(dto);

    public static MemberDto? Map(MemberEntity? entity)
    {
        if (entity is null)
            return null;

        return new MemberDto
        {
            Name = entity.Name,
            MusicBrainzID = entity.MusicBrainzID,
        };
    }

    public static MemberEntity? Map(MemberDto? dto)
    {
        if (dto is null)
            return null;

        return new MemberEntity
        {
            Name = dto.Name,
            MusicBrainzID = dto.MusicBrainzID,
        };
    }
}