using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MusicData.Application.Interfaces;
using NSubstitute;

namespace MusicData.Tests.Api.E2E;

public sealed class MusicDataApiFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key-001";

    /// <summary>
    /// The aggregator the test grid talks to. Pre-configured to return null for
    /// every call; individual tests override the relevant return values via
    /// NSubstitute's Returns(...) before issuing their HTTP request.
    /// </summary>
    public IMusicAggregator MusicAggregator { get; } = Substitute.For<IMusicAggregator>();

    public ILyricsAggregator LyricsAggregator { get; } = Substitute.For<ILyricsAggregator>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Replaces appsettings.Development.json values so the host runs
            // self-contained: in-memory LiteDB, deterministic API key, very
            // generous IP rate limit so individual test bursts don't trip 429.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ":memory:",
                ["ApiKeySettings:Key"] = TestApiKey,
                ["RateLimiting:RequestsPerMinute"] = "10000",

                // Disable every external service in the test environment —
                // tests inject IMusicAggregator / ILyricsAggregator stubs
                // before issuing their request, so the underlying HTTP
                // services are never reached.
                ["Services:LastFM:Enabled"] = "false",
                ["Services:MusicBrainz:Enabled"] = "false",
                ["Services:Fanart:Enabled"] = "false",
                ["Services:CoverArt:Enabled"] = "false",
                ["Services:LyricsOvh:Enabled"] = "false",
                ["Services:LrcLib:Enabled"] = "false",

                ["Services:LastFM:BaseUrl"] = "https://example.test/lastfm",
                ["Services:MusicBrainz:BaseUrl"] = "https://example.test/musicbrainz",
                ["Services:Fanart:BaseUrl"] = "https://example.test/fanart",
                ["Services:CoverArt:BaseUrl"] = "https://example.test/coverart",
                ["Services:LyricsOvh:BaseUrl"] = "https://example.test/lyricsovh",
                ["Services:LrcLib:BaseUrl"] = "https://example.test/lrclib",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IMusicAggregator>();
            services.AddSingleton(MusicAggregator);

            services.RemoveAll<ILyricsAggregator>();
            services.AddSingleton(LyricsAggregator);

            // The IMusicBrainzLookup factory in production resolves
            // MusicBrainzService out of IEnumerable<IMusicService>, but we've
            // disabled all music services above so the factory would fail
            // with InvalidOperationException ("Sequence contains no elements").
            // Tests don't exercise the lookup path; provide a no-op stand-in.
            services.RemoveAll<MusicData.Infrastructure.Services.MusicBrainz.IMusicBrainzLookup>();
            services.AddSingleton<MusicData.Infrastructure.Services.MusicBrainz.IMusicBrainzLookup>(
                Substitute.For<MusicData.Infrastructure.Services.MusicBrainz.IMusicBrainzLookup>());
        });
    }


    public HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
        return client;
    }
}
