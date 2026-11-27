using System.Net;
using System.Text;
using Hqub.MusicBrainz;
using Microsoft.Extensions.Logging.Abstractions;
using MusicData.Application.DTOs;
using MusicData.Infrastructure.Services.MusicBrainz;

namespace MusicData.Tests.Infrastructure.Services;

public class MusicBrainzServiceTests
{
    private const string Mbid = "67f66c07-6e61-4026-ade5-7e782fad3a5d";

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetArtist_UnknownOrMalformedMbid_ReturnsNull_InsteadOfFailing(HttpStatusCode status)
    {
        MusicBrainzService sut = Create(new StubHandler(_ => Json(status, """{"error":"Not Found","help":"..."}""")));

        Assert.Null(await sut.GetArtistAsync(Mbid, CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetAlbum_UnknownOrMalformedMbid_ReturnsNull_InsteadOfFailing(HttpStatusCode status)
    {
        MusicBrainzService sut = Create(new StubHandler(_ => Json(status, """{"error":"Not Found","help":"..."}""")));

        Assert.Null(await sut.GetAlbumAsync(Mbid, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetArtist_ServerError_Throws_InsteadOfLookingLikeNotFound()
    {
        MusicBrainzService sut = Create(new StubHandler(_ => Json(HttpStatusCode.ServiceUnavailable, """{"error":"Rate limited"}""")));

        await Assert.ThrowsAsync<WebServiceException>(() => sut.GetArtistAsync(Mbid, CancellationToken.None));
    }

    [Fact]
    public async Task GetArtist_Found_MapsIdentity()
    {
        StubHandler handler = new(_ => Json(HttpStatusCode.OK, $$"""{"id":"{{Mbid}}","name":"Foo Fighters","country":"US"}"""));

        var artist = await Create(handler).GetArtistAsync(Mbid, CancellationToken.None);

        Assert.NotNull(artist);
        Assert.Equal("Foo Fighters", artist!.Name);
        Assert.Equal(Mbid, artist.MusicBrainzID);
        Assert.Contains($"artist/{Mbid}", Assert.Single(handler.Urls));
    }

    [Fact]
    public async Task GetAlbum_MapsArtistAndReleaseGroup_FromTheRelease()
    {
        StubHandler handler = new(_ => Json(HttpStatusCode.OK, $$"""
            {
              "id": "{{Mbid}}",
              "title": "Abbey Road",
              "date": "1969-09-26",
              "artist-credit": [ { "name": "The Beatles", "artist": { "id": "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d", "name": "The Beatles" } } ],
              "release-group": { "id": "9162580e-5df4-32de-80cc-f45a8d8a9b1d", "first-release-date": "1969-09-26" }
            }
            """));

        var album = await Create(handler).GetAlbumAsync(Mbid, null, CancellationToken.None);

        Assert.Equal("Abbey Road", album!.Name);
        Assert.Equal("b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d", album.MusicBrainzArtistID);
        Assert.Equal("9162580e-5df4-32de-80cc-f45a8d8a9b1d", album.ReleaseGroupMusicBrainzID);
        string url = Assert.Single(handler.Urls);
        Assert.Contains("artist-credits", url);
        Assert.Contains("release-groups", url);
    }

    [Fact]
    public async Task FindArtist_ServerError_Throws_InsteadOfLookingLikeNoMatch()
    {
        MusicBrainzService sut = Create(new StubHandler(_ => Json(HttpStatusCode.ServiceUnavailable, """{"error":"Rate limited"}""")));

        await Assert.ThrowsAsync<WebServiceException>(() => sut.FindArtistAsync("Foo Fighters", CancellationToken.None));
    }

    [Fact]
    public async Task FindAlbum_ServerError_Throws_InsteadOfLookingLikeNoMatch()
    {
        MusicBrainzService sut = Create(new StubHandler(_ => Json(HttpStatusCode.BadGateway, "{}")));

        await Assert.ThrowsAsync<WebServiceException>(() => sut.FindAlbumAsync("Wasting Light", Mbid, CancellationToken.None));
    }

    [Fact]
    public async Task FindArtist_NoMatch_ReturnsNull()
    {
        MusicBrainzService sut = Create(new StubHandler(_ => Json(HttpStatusCode.OK, """{"count":0,"offset":0,"artists":[]}""")));

        Assert.Null(await sut.FindArtistAsync("zzzz", CancellationToken.None));
    }

    [Fact]
    public async Task FindArtist_Match_ReturnsMbid()
    {
        MusicBrainzService sut = Create(new StubHandler(_ => Json(
            HttpStatusCode.OK,
            $$"""{"count":1,"offset":0,"artists":[{"id":"{{Mbid}}","score":100,"name":"Foo Fighters"}]}""")));

        Assert.Equal(Mbid, await sut.FindArtistAsync("Foo Fighters", CancellationToken.None));
    }

    [Fact]
    public async Task CallerCancellation_AbortsTheInFlightMusicBrainzRequest()
    {
        using CancellationTokenSource cts = new();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        MusicBrainzService sut = Create(new HangingHandler(started));

        Task<ArtistDto?> pending = sut.GetArtistAsync(Mbid, cts.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
    }


    private sealed class HangingHandler(TaskCompletionSource started) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }


    internal static MusicBrainzService Create(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://musicbrainz.test/ws/2/") },
        NullLogger<MusicBrainzService>.Instance);

    internal static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };


    internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> route) : HttpMessageHandler
    {
        public List<string> Urls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Urls.Add(request.RequestUri!.ToString());
            return Task.FromResult(route(request));
        }
    }
}
