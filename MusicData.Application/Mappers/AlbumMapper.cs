using MusicData.Application.DTOs;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Mappers;

public static class AlbumMapper
{
    public static AlbumDto ToDto(this AlbumEntity entity) => Map(entity);

    public static AlbumEntity ToEntity(this AlbumDto dto) => Map(dto);

    public static AlbumDto Map(AlbumEntity entity)
    {
        if (entity is null) return null!;

        AlbumDto dto = new()
        {
            AllMusicID = entity.AllMusicID,
            AmazonID = entity.AmazonID,
            AudioDbArtistID = entity.AudioDbArtistID,
            AudioDbID = entity.AudioDbID,
            MusicBrainzArtistID = entity.MusicBrainzArtistID,
            Biography = entity.Biography,
            DiscogsID = entity.DiscogsID,
            GeniusID = entity.GeniusID,
            Genre = entity.Genre,
            Label = entity.Label,
            LastFM = entity.LastFM,
            LyricWikiID = entity.LyricWikiID,
            Artist = entity.Artist,
            MusicBrainzID = entity.MusicBrainzID,
            Name = entity.Name,
            ReleaseDate = entity.ReleaseDate,
            ReleaseFormat = entity.ReleaseFormat,
            ReleaseGroupMusicBrainzID = entity.ReleaseGroupMusicBrainzID,
            Sales = entity.Sales,
            Score = entity.Score,
            Wikipedia = entity.Wikipedia,
            WikipediaID = entity.WikipediaID,
            WikidataID = entity.WikidataID,
            Year = entity.Year,
            MusicMozID = entity.MusicMozID,
            PictureUrl = entity.PictureUrl
        };

        dto.Tracks = entity.Tracks.Select(TrackMapper.ToDto).Where(t => t is not null)!.ToList()!;

        return dto;
    }


    public static AlbumEntity Map(AlbumDto dto)
    {
        AlbumEntity entity = new()
        {
            AllMusicID = dto.AllMusicID,
            AmazonID = dto.AmazonID,
            AudioDbArtistID = dto.AudioDbArtistID,
            AudioDbID = dto.AudioDbID,
            MusicBrainzArtistID = dto.MusicBrainzArtistID,
            Biography = dto.Biography,
            DiscogsID = dto.DiscogsID,
            GeniusID = dto.GeniusID,
            Genre = dto.Genre,
            Label = dto.Label,
            LastFM = dto.LastFM,
            LyricWikiID = dto.LyricWikiID,
            Artist = dto.Artist,
            MusicBrainzID = dto.MusicBrainzID,
            Name = dto.Name,
            ReleaseDate = dto.ReleaseDate,
            ReleaseFormat = dto.ReleaseFormat,
            ReleaseGroupMusicBrainzID = dto.ReleaseGroupMusicBrainzID,
            Sales = dto.Sales,
            Score = dto.Score,
            Wikipedia = dto.Wikipedia,
            WikipediaID = dto.WikipediaID,
            WikidataID = dto.WikidataID,
            Year = dto.Year,
            MusicMozID = dto.MusicMozID,
            PictureUrl = dto.PictureUrl
        };

        entity.Tracks = dto.Tracks?.Select(TrackMapper.ToEntity).Where(t => t is not null)!.ToList()!;

        return entity;
    }
}