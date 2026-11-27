using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MusicData.Application.DTOs;
using MusicData.Infrastructure.Services.LastFm;

namespace MusicData.Tests.Infrastructure.Services;

public class LastFmServiceTests
{
    [Fact]
    public async Task GetAlbum_WithoutTracks_ReturnsAlbumWithNoTracks()
    {
        AlbumDto? album = await GetAlbumAsync("""{"album":{"name":"Solo","artist":"Foo","mbid":"m"}}""");

        Assert.NotNull(album);
        Assert.Equal("Solo", album!.Name);
        Assert.Empty(album.Tracks);
    }

    [Fact]
    public async Task GetAlbum_SingleTrackAsObject_IsReadAsOneTrack()
    {
        AlbumDto? album = await GetAlbumAsync(
            """{"album":{"name":"Single","artist":"Foo","tracks":{"track":{"name":"Only","duration":123,"@attr":{"rank":1}}}}}""");

        TrackDto track = Assert.Single(album!.Tracks);
        Assert.Equal("Only", track.Name);
        Assert.Equal(123, track.Duration);
        Assert.Equal(1, track.Position);
    }

    [Fact]
    public async Task GetAlbum_TrackArray_WithMissingAttrAndNullDuration_FallsBackToListOrder()
    {
        AlbumDto? album = await GetAlbumAsync(
            """{"album":{"name":"LP","artist":"Foo","tracks":{"track":[{"name":"A","duration":null},{"name":"B","duration":"200","@attr":{"rank":"2"}}]}}}""");

        Assert.Equal(2, album!.Tracks.Count);
        Assert.Equal(("A", 1, (int?)null), (album.Tracks[0].Name, album.Tracks[0].Position, album.Tracks[0].Duration));
        Assert.Equal(("B", 2, (int?)200), (album.Tracks[1].Name, album.Tracks[1].Position, album.Tracks[1].Duration));
    }

    [Fact]
    public async Task GetAlbum_ErrorPayloadWithoutAlbum_ReturnsNull()
    {
        Assert.Null(await GetAlbumAsync("""{"error":6,"message":"Album not found"}"""));
    }


    private static Task<AlbumDto?> GetAlbumAsync(string json)
    {
        LastFmService sut = new(
            new HttpClient(new JsonHandler(json)) { BaseAddress = new Uri("https://lastfm.test/2.0/") },
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
            Options.Create(new LastFmSettings { BaseUrl = "https://lastfm.test/2.0/", ApiKey = "k" }),
            NullLogger<LastFmService>.Instance);

        return sut.GetAlbumAsync("98cfdc56-7acf-4ecb-8b42-71c5b1c7421b", null, CancellationToken.None);
    }


    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
