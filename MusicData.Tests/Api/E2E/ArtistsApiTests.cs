using System.Net;
using System.Net.Http.Json;
using MusicData.Application.DTOs;
using NSubstitute;

namespace MusicData.Tests.Api.E2E;

public class ArtistsApiTests : IClassFixture<MusicDataApiFactory>
{
    private readonly MusicDataApiFactory _factory;

    public ArtistsApiTests(MusicDataApiFactory factory)
    {
        _factory = factory;
    }


    [Fact]
    public async Task ByName_EmptyName_ReturnsBadRequest()
    {
        // Empty path segment matches a different route or 404; use a too-long
        // name to hit the validator deterministically.
        using HttpClient client = _factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync("/v1/artists/byName/" + new string('a', 256));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ByName_AggregatorReturnsArtist_Returns200WithDto()
    {
        ArtistDto stub = new() { Name = "Foo Fighters", MusicBrainzID = "mbid-foo", Origin = "Aggregated" };
        _factory.MusicAggregator
            .GetArtistByNameAsync("Foo Fighters", Arg.Any<CancellationToken>())
            .Returns(stub);

        using HttpClient client = _factory.CreateAuthenticatedClient();
        HttpResponseMessage response = await client.GetAsync("/v1/artists/byName/Foo Fighters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ArtistDto? body = await response.Content.ReadFromJsonAsync<ArtistDto>();
        Assert.NotNull(body);
        Assert.Equal("Foo Fighters", body!.Name);
        Assert.Equal("mbid-foo", body.MusicBrainzID);
    }

    [Fact]
    public async Task ByName_HappyPath_SetsPublicCacheHeaders()
    {
        _factory.MusicAggregator
            .GetArtistByNameAsync("Cached Headers Test", Arg.Any<CancellationToken>())
            .Returns(new ArtistDto { Name = "Cached Headers Test", MusicBrainzID = "mbid-headers" });

        using HttpClient client = _factory.CreateAuthenticatedClient();
        HttpResponseMessage response = await client.GetAsync("/v1/artists/byName/Cached Headers Test");

        Assert.Equal("public, max-age=30", response.Headers.CacheControl?.ToString());
        Assert.Contains("Accept-Encoding", response.Headers.Vary);
    }

    [Fact]
    public async Task ByMbid_TooLong_ReturnsBadRequest()
    {
        using HttpClient client = _factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync("/v1/artists/byMbid/" + new string('a', 37));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ByMbid_AggregatorReturnsNull_Returns404()
    {
        _factory.MusicAggregator
            .GetArtistByMusicBrainzIdAsync("ghost-mbid-0000000000000000000000000", Arg.Any<CancellationToken>())
            .Returns((ArtistDto?)null);

        using HttpClient client = _factory.CreateAuthenticatedClient();
        HttpResponseMessage response = await client.GetAsync("/v1/artists/byMbid/ghost-mbid-0000000000000000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
