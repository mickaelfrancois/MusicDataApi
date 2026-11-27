using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.Services;
using MusicData.Infrastructure.Services.LrcLib;

namespace MusicData.Tests.Infrastructure.Services;

public class HttpClientRegistrationTests
{
    [Fact]
    public void ExternalClient_RecyclesPooledConnections_SoDnsChangesArePickedUp()
    {
        // The typed clients are captured for the process lifetime by singleton
        // aggregators, so IHttpClientFactory's handler rotation never applies.
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Services:LrcLib:Enabled"] = "true",
                ["Services:LrcLib:BaseUrl"] = "https://lrclib.test",
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddExternalHttpClient<ILyricsService, LrcLibService, LrcLibSettings>(configuration, "lrclib", "Services:LrcLib");
        using ServiceProvider provider = services.BuildServiceProvider();

        HttpMessageHandler handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler("lrclib");
        while (handler is DelegatingHandler delegating)
            handler = delegating.InnerHandler!;

        SocketsHttpHandler primary = Assert.IsType<SocketsHttpHandler>(handler);
        Assert.InRange(primary.PooledConnectionLifetime, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(15));
    }
}
