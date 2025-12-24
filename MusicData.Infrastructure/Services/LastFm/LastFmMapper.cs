using MusicData.Application.DTOs;

namespace MusicData.Infrastructure.Services.LastFm;

internal static class LastFmMapper
{
    public static AlbumDto Map(LastFmAlbum album)
    {
        LastFmImage? image = GetPicture(album.Image);

        List<TrackDto>? tracks = GetTracks(album.Tracks);

        return new AlbumDto()
        {
            Name = album.Name,
            MusicBrainzID = album.Mbid,
            Artist = album.Artist,
            Tracks = tracks
        };
    }


    public static ArtistDto Map(LastFmArtist artist)
    {
        LastFmImage? image = GetPicture(artist.Image);

        return new ArtistDto()
        {
            Name = artist.Name,
            MusicBrainzID = artist.Mbid,
            LastFM = artist.Url,
            Biography = artist.Bio?.Content ?? string.Empty,
        };
    }

    private static LastFmImage? GetPicture(List<LastFmImage>? images)
    {
        LastFmImage? image = default;

        if (images is not null)
        {
            image = images.FirstOrDefault(c => c.Size == "extralarge");
            image ??= images.FirstOrDefault(c => c.Size == "mega");
            image ??= images.FirstOrDefault(c => c.Size == "");
        }

        return image;
    }

    private static List<TrackDto> GetTracks(LastFmTracks tracks)
    {
        List<TrackDto> trackDtos = [];

        if (tracks.Track is not null)
        {
            foreach (LastFmTrack track in tracks.Track)
            {
                trackDtos.Add(new TrackDto()
                {
                    Name = track.Name,
                    Duration = track.Duration,
                    Position = track.Attr.Rank,
                });
            }
        }

        return trackDtos;
    }
}
