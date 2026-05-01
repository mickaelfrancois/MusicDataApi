using System.Net;
using System.Net.Http.Json;
using MusicData.Application.DTOs;
using NSubstitute;

namespace MusicData.Tests.Api.E2E;

public class AlbumsApiTests : IClassFixture<MusicDataApiFactory>
{
    private readonly MusicDataApiFactory _factory;

    public AlbumsApiTests(MusicDataApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ByMbid_AggregatorReturnsAlbum_Returns200WithDto()
    {
        AlbumDto stub = new()
        {
            Name = "Wasting Light",
            MusicBrainzID = "album-mbid",
            MusicBrainzArtistID = "artist-mbid",
            Origin = "Aggregated"
        };
        _factory.MusicAggregator
            .GetAlbumByMusicBrainzIdAsync("album-mbid", "artist-mbid", Arg.Any<CancellationToken>())
            .Returns(stub);

        using HttpClient client = _factory.CreateAuthenticatedClient();
        HttpResponseMessage response = await client.GetAsync("/v1/albums/byMbid/artist-mbid/album-mbid");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AlbumDto? body = await response.Content.ReadFromJsonAsync<AlbumDto>();
        Assert.NotNull(body);
        Assert.Equal("Wasting Light", body!.Name);
    }

    [Fact]
    public async Task ByMbid_TooLongAlbumMbid_ReturnsBadRequest()
    {
        using HttpClient client = _factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync(
            "/v1/albums/byMbid/short/" + new string('a', 37));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
