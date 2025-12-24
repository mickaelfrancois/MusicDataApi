using MusicData.Application.DTOs;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Mappers;

public static class ArtistMapper
{
    public static ArtistDto ToDto(this ArtistEntity entity) => Map(entity);

    public static ArtistEntity ToEntity(this ArtistDto dto) => Map(dto);



    public static ArtistDto Map(ArtistEntity entity)
    {
        ArtistDto dto = new()
        {
            AllMusic = entity.AllMusic,
            AudioDbID = entity.AudioDbID,
            Bandsintown = entity.Bandsintown,
            BannerUrl = entity.BannerUrl,
            Biography = entity.Biography,
            CountryCode = entity.CountryCode,
            EndYear = entity.EndYear,
            Disbanded = entity.Disbanded,
            Discogs = entity.Discogs,
            Fanart2Url = entity.Fanart2Url,
            Fanart3Url = entity.Fanart3Url,
            FanartUrl = entity.FanartUrl,
            Facebook = entity.Facebook,
            BeginYear = entity.BeginYear,
            Flickr = entity.Flickr,
            MusicBrainzID = entity.MusicBrainzID,
            Instagram = entity.Instagram,
            LogoUrl = entity.LogoUrl,
            Name = entity.Name,
            PictureUrl = entity.PictureUrl,
            Twitter = entity.Twitter,
            Website = entity.Website,
            Youtube = entity.Youtube,
            LastFM = entity.LastFM
        };

        dto.Members = entity.Members.Select(MemberMapper.ToDto).Where(m => m is not null)!.ToList()!;

        return dto;
    }


    public static ArtistEntity Map(ArtistDto dto)
    {
        ArtistEntity entity = new()
        {
            AllMusic = dto.AllMusic,
            AudioDbID = dto.AudioDbID,
            Bandsintown = dto.Bandsintown,
            BannerUrl = dto.BannerUrl,
            Biography = dto.Biography,
            CountryCode = dto.CountryCode,
            EndYear = dto.EndYear,
            Disbanded = dto.Disbanded,
            Discogs = dto.Discogs,
            Fanart2Url = dto.Fanart2Url,
            Fanart3Url = dto.Fanart3Url,
            FanartUrl = dto.FanartUrl,
            Facebook = dto.Facebook,
            BeginYear = dto.BeginYear,
            Flickr = dto.Flickr,
            MusicBrainzID = dto.MusicBrainzID,
            Instagram = dto.Instagram,
            LogoUrl = dto.LogoUrl,
            Name = dto.Name,
            PictureUrl = dto.PictureUrl,
            Twitter = dto.Twitter,
            Website = dto.Website,
            Youtube = dto.Youtube,
            LastFM = dto.LastFM,
        };

        entity.Members = dto.Members?.Select(MemberMapper.ToEntity).Where(m => m is not null)!.ToList()!;

        return entity;
    }
}