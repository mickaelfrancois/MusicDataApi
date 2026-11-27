using System.Net;
using System.Net.Http.Json;
using MusicData.Application.DTOs;
using NSubstitute;

namespace MusicData.Tests.Api.E2E;

public class AlbumsApiTests : IClassFixture<MusicDataApiFactory>
{
    private const string ArtistMbid = "67f66c07-6e61-4026-ade5-7e782fad3a5d";
    private const string AlbumMbid = "98cfdc56-7acf-4ecb-8b42-71c5b1c7421b";

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
            MusicBrainzID = AlbumMbid,
            MusicBrainzArtistID = ArtistMbid,
            Origin = "Aggregated"
        };
        _factory.MusicAggregator
            .GetAlbumByMusicBrainzIdAsync(AlbumMbid, ArtistMbid, Arg.Any<CancellationToken>())
            .Returns(stub);

        using HttpClient client = _factory.CreateAuthenticatedClient();
        HttpResponseMessage response = await client.GetAsync($"/v1/albums/byMbid/{ArtistMbid}/{AlbumMbid}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AlbumDto? body = await response.Content.ReadFromJsonAsync<AlbumDto>();
        Assert.NotNull(body);
        Assert.Equal("Wasting Light", body!.Name);
    }

    [Theory]
    [InlineData("not-a-guid", AlbumMbid)]
    [InlineData(ArtistMbid, "not-a-guid")]
    public async Task ByMbid_NotAGuid_ReturnsBadRequest(string artistMbid, string albumMbid)
    {
        using HttpClient client = _factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync($"/v1/albums/byMbid/{artistMbid}/{albumMbid}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
