using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MusicData.Application.DTOs;
using MusicData.Infrastructure.Services.LyricsOvh;

namespace MusicData.Tests.Infrastructure.Services;

public class LyricsOvhServiceTests
{
    [Theory]
    [InlineData("""{"lyrics":""}""")]
    [InlineData("""{"lyrics":"  \n "}""")]
    [InlineData("""{"lyrics":null}""")]
    [InlineData("{}")]
    public async Task EmptyLyrics_ReturnsNull_InsteadOfEmptyLyrics(string json)
    {
        Assert.Null(await Create(json).GetLyricsAsync("Walk", "Foo", "", 0, CancellationToken.None));
    }

    [Fact]
    public async Task Lyrics_AreTrimmedAndReturned()
    {
        LyricsDto? lyrics = await Create("""{"lyrics":"la la\n"}""").GetLyricsAsync("Walk", "Foo", "", 0, CancellationToken.None);

        Assert.Equal("la la", lyrics!.PlainLyrics);
    }


    private static LyricsOvhService Create(string json) => new(
        new HttpClient(new JsonHandler(json)) { BaseAddress = new Uri("https://lyricsovh.test/v1/") },
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
        NullLogger<LyricsOvhService>.Instance);

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
