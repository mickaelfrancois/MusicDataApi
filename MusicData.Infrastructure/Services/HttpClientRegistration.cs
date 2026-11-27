using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MusicData.Infrastructure.Services;

internal static class HttpClientRegistration
{
    private const string UserAgent = "RoK/1.0 (rok@francois.ovh)";
    private static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(5);

    public static IServiceCollection AddExternalHttpClient<TInterface, TImpl, TSettings>(
        this IServiceCollection services,
        IConfiguration configuration,
        string name,
        string configSection,
        Action<HttpClient>? configureClient = null,
        int retriesOnBusy = 0)
        where TInterface : class
        where TImpl : class, TInterface
        where TSettings : class, IHttpServiceSettings, new()
    {
        IConfigurationSection section = configuration.GetSection(configSection);
        services.Configure<TSettings>(section);

        // Disabled services don't enter the container at all — the aggregator's
        // IEnumerable<TInterface> simply won't see them, no per-call short-circuit
        // is needed inside the implementation.
        if (!section.GetValue<bool>("Enabled"))
            return services;

        IHttpClientBuilder builder = services.AddHttpClient<TInterface, TImpl>(name, (sp, client) =>
        {
            TSettings settings = sp.GetRequiredService<IOptions<TSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgent);
            configureClient?.Invoke(client);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

            // The typed clients are captured for the process lifetime by the
            // singleton aggregators, so IHttpClientFactory never rotates their
            // handler: recycle connections here or DNS changes are never seen.
            PooledConnectionLifetime = PooledConnectionLifetime
        });

        if (retriesOnBusy > 0)
            builder.AddHttpMessageHandler(() => new RetryOnBusyHandler(retriesOnBusy));

        return services;
    }
}
