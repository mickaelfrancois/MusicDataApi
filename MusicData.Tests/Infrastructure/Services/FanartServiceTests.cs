using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MusicData.Infrastructure.Services.Fanart;

namespace MusicData.Tests.Infrastructure.Services;

public class FanartServiceTests
{
    [Fact]
    public async Task GetArtist_EscapesMusicBrainzIdAndApiKey_InRequestUrl()
    {
        CapturingHandler handler = new(HttpStatusCode.NotFound);
        FanartService sut = new(
            new HttpClient(handler) { BaseAddress = new Uri("https://fanart.test/v3.2/music/") },
            new JsonSerializerOptions(),
            Options.Create(new FanartSettings { ApiKey = "k&e=y" }),
            NullLogger<FanartService>.Instance);

        await sut.GetArtistAsync("abc/../x?evil=1#f", CancellationToken.None);

        Uri sent = Assert.Single(handler.Requests);
        Assert.Equal("fanart.test", sent.Host);
        Assert.StartsWith("/v3.2/music/abc%2F..%2Fx%3Fevil%3D1%23f", sent.AbsolutePath);
        Assert.Equal("?api_key=k%26e%3Dy", sent.Query);
    }

    [Fact]
    public async Task GetArtist_ServerError_Throws_InsteadOfLookingLikeNotFound()
    {
        FanartService sut = new(
            new HttpClient(new CapturingHandler(HttpStatusCode.BadGateway)) { BaseAddress = new Uri("https://fanart.test/") },
            new JsonSerializerOptions(),
            Options.Create(new FanartSettings { ApiKey = "k" }),
            NullLogger<FanartService>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetArtistAsync("mbid", CancellationToken.None));
    }

    [Fact]
    public async Task GetArtist_NotFound_ReturnsNull()
    {
        FanartService sut = new(
            new HttpClient(new CapturingHandler(HttpStatusCode.NotFound)) { BaseAddress = new Uri("https://fanart.test/") },
            new JsonSerializerOptions(),
            Options.Create(new FanartSettings { ApiKey = "k" }),
            NullLogger<FanartService>.Instance);

        Assert.Null(await sut.GetArtistAsync("mbid", CancellationToken.None));
    }

    [Fact]
    public async Task GetArtist_WithoutAnyImage_ReturnsNull_InsteadOfAnEmptyDto()
    {
        FanartService sut = Create("""{"name":"Foo","mbid_id":"x","artistbackground":[]}""");

        Assert.Null(await sut.GetArtistAsync("mbid", CancellationToken.None));
        Assert.Null(await sut.GetAlbumAsync("release", "release-group", CancellationToken.None));
    }

    [Fact]
    public async Task GetArtist_WithImages_MapsThem()
    {
        FanartService sut = Create("""{"artistthumb":[{"url":"https://img/thumb.jpg","likes":"3"}]}""");

        Assert.Equal("https://img/thumb.jpg", (await sut.GetArtistAsync("mbid", CancellationToken.None))!.PictureUrl);
        Assert.Equal("https://img/thumb.jpg", (await sut.GetAlbumAsync("release", "release-group", CancellationToken.None))!.PictureUrl);
    }


    private static FanartService Create(string json) => new(
        new HttpClient(new CapturingHandler(HttpStatusCode.OK, json)) { BaseAddress = new Uri("https://fanart.test/") },
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
        Options.Create(new FanartSettings { ApiKey = "k" }),
        NullLogger<FanartService>.Instance);


    private sealed class CapturingHandler(HttpStatusCode status, string? json = null) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            HttpResponseMessage response = new(status);
            if (json is not null)
                response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            return Task.FromResult(response);
        }
    }
}
