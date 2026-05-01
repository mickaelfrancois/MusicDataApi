using System.Net;
using System.Net.Http.Json;
using MusicData.Application.DTOs;
using NSubstitute;

namespace MusicData.Tests.Api.E2E;

public class LyricsApiTests : IClassFixture<MusicDataApiFactory>
{
    private readonly MusicDataApiFactory _factory;

    public LyricsApiTests(MusicDataApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TitleTooLong_Returns400()
    {
        using HttpClient client = _factory.CreateAuthenticatedClient();
        string longTitle = new('a', 256);

        HttpResponseMessage response = await client.GetAsync(
            $"/v1/lyrics?title={longTitle}&artistName=Foo&albumName=Bar&duration=200");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AggregatorReturnsLyrics_Returns200WithDto()
    {
        LyricsDto stub = new()
        {
            Title = "Walk",
            ArtistName = "Foo Fighters",
            AlbumName = "Wasting Light",
            Duration = 257,
            PlainLyrics = "I never wanna die",
            Origin = "LrcLib"
        };
        _factory.LyricsAggregator
            .GetLyricsAsync("Walk", "Foo Fighters", "Wasting Light", 257, Arg.Any<CancellationToken>())
            .Returns(stub);

        using HttpClient client = _factory.CreateAuthenticatedClient();
        HttpResponseMessage response = await client.GetAsync(
            "/v1/lyrics?title=Walk&artistName=Foo Fighters&albumName=Wasting Light&duration=257");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LyricsDto? body = await response.Content.ReadFromJsonAsync<LyricsDto>();
        Assert.NotNull(body);
        Assert.Equal("Walk", body!.Title);
        Assert.Equal("Wasting Light", body.AlbumName);
        Assert.Equal(257, body.Duration);
    }
}
