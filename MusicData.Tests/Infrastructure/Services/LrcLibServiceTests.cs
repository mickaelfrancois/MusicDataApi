using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MusicData.Application.DTOs;
using MusicData.Infrastructure.Services.LrcLib;

namespace MusicData.Tests.Infrastructure.Services;

public class LrcLibServiceTests
{
    private const string Hit = """{"id":1,"trackName":"Walk","artistName":"Foo","albumName":"A","duration":257.4,"plainLyrics":null,"syncedLyrics":"[00:01] hi"}""";

    [Fact]
    public async Task NoAlbumOrDuration_UsesSearchOnly_WithoutAlbumParam_AndToleratesNullPlainLyrics()
    {
        RouteHandler handler = new(req => req.RequestUri!.AbsolutePath.EndsWith("/search")
            ? Json($"[{Hit}]")
            : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        LyricsDto? result = await Create(handler).GetLyricsAsync("Walk", "Foo", "", 0, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("[00:01] hi", result!.SyncLyrics);
        Assert.Equal(string.Empty, result.PlainLyrics);
        string url = Assert.Single(handler.Urls);
        Assert.Contains("api/search", url);
        Assert.DoesNotContain("album_name", url);
    }

    [Fact]
    public async Task AlbumAndDuration_TryGetFirst_ThenFallBackToSearchOn404()
    {
        RouteHandler handler = new(req => req.RequestUri!.AbsolutePath.EndsWith("/get")
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : Json($"[{Hit}]"));

        LyricsDto? result = await Create(handler).GetLyricsAsync("Walk", "Foo", "A", 257, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, handler.Urls.Count);
    }


    private static LrcLibService Create(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://lrclib.test/") },
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
        NullLogger<LrcLibService>.Instance);

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };


    private sealed class RouteHandler(Func<HttpRequestMessage, HttpResponseMessage> route) : HttpMessageHandler
    {
        public List<string> Urls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Urls.Add(request.RequestUri!.ToString());
            return Task.FromResult(route(request));
        }
    }
}
