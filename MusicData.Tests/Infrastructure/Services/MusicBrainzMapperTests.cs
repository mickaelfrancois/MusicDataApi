using Hqub.MusicBrainz.Entities;
using MusicData.Application.DTOs;
using MusicData.Infrastructure.Services.MusicBrainz;

namespace MusicData.Tests.Infrastructure.Services;

public class MusicBrainzMapperTests
{
    [Fact]
    public void MapArtist_RelationWithoutType_IsIgnored_InsteadOfThrowing()
    {
        Artist artist = new()
        {
            Id = "id",
            Name = "Band",
            Relations =
            [
                new Relation { TargetType = "artist", Type = null, Artist = new Artist { Id = "x", Name = "Untyped" } },
                new Relation { TargetType = "artist", Type = "member of band", Artist = new Artist { Id = "m", Name = "Member" } }
            ]
        };

        ArtistDto? dto = MusicBrainzMapper.Map(artist);

        MemberDto member = Assert.Single(dto!.Members);
        Assert.Equal("Member", member.Name);
    }
}
