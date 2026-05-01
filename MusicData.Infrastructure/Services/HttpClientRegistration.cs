using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MusicData.Infrastructure.Services;

internal static class HttpClientRegistration
{
    private const string UserAgent = "RoK/1.0 (rok@francois.ovh)";

    public static IServiceCollection AddExternalHttpClient<TInterface, TImpl, TSettings>(
        this IServiceCollection services,
        IConfiguration configuration,
        string name,
        string configSection,
        Action<HttpClient>? configureClient = null)
        where TInterface : class
        where TImpl : class, TInterface
        where TSettings : class, IHttpServiceSettings, new()
    {
        services.Configure<TSettings>(configuration.GetSection(configSection));

        services.AddHttpClient<TInterface, TImpl>(name, (sp, client) =>
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
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });

        return services;
    }
}
