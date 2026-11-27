using System.Net;
using System.Text;
using Hqub.MusicBrainz;
using Microsoft.Extensions.Logging.Abstractions;
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
