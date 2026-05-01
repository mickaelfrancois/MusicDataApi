using MusicData.Application.DTOs;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Tests.Application.Mappers;

public class LyricsMapperTests
{
    [Fact]
    public void DtoToEntity_PreservesIdentityFields()
    {
        LyricsDto dto = new()
        {
            Title = "Walk",
            ArtistName = "Foo Fighters",
            PlainLyrics = "I never wanna die...",
            SyncLyrics = "[00:01.00]I never...",
        };

        LyricsEntity entity = dto.ToEntity();

        Assert.Equal("Walk", entity.Title);
        Assert.Equal("Foo Fighters", entity.ArtistName);
        Assert.Equal(dto.PlainLyrics, entity.PlainLyrics);
        Assert.Equal(dto.SyncLyrics, entity.SyncLyrics);
    }

    [Fact]
    public void EntityToDto_PreservesIdentityFields()
    {
        LyricsEntity entity = new()
        {
            Title = "Walk",
            ArtistName = "Foo Fighters",
            PlainLyrics = "lyrics",
        };

        LyricsDto dto = entity.ToDto();

        Assert.Equal("Walk", dto.Title);
        Assert.Equal("Foo Fighters", dto.ArtistName);
        Assert.Equal("lyrics", dto.PlainLyrics);
    }

    [Fact]
    public void RoundTrip_PreservesAlbumNameAndDuration()
    {
        // P2-2 regression guard: AlbumName and Duration used to be silently
        // dropped on cache because LyricsEntity didn't carry them. Now it does.
        LyricsDto dto = new()
        {
            Title = "Walk",
            ArtistName = "Foo Fighters",
            AlbumName = "Wasting Light",
            Duration = 257,
        };

        LyricsDto roundTripped = dto.ToEntity().ToDto();

        Assert.Equal("Wasting Light", roundTripped.AlbumName);
        Assert.Equal(257, roundTripped.Duration);
    }
}
