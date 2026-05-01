using System.Threading.RateLimiting;
using LiteDB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.Concurrency;
using MusicData.Infrastructure.HealthChecks;
using MusicData.Infrastructure.RateLimiting;
using MusicData.Infrastructure.Repositories;
using MusicData.Infrastructure.Security;
using MusicData.Infrastructure.Services;
using MusicData.Infrastructure.Services.CoverArt;
using MusicData.Infrastructure.Services.Fanart;
using MusicData.Infrastructure.Services.LastFm;
using MusicData.Infrastructure.Services.LrcLib;
using MusicData.Infrastructure.Services.LyricsOvh;
using MusicData.Infrastructure.Services.MusicBrainz;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MusicData.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddDataContext(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetConnectionString("DefaultConnection"));

        services.AddSingleton<ILiteDatabase, LiteDatabase>(c =>
        {
            return new LiteDatabase(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<ILyricsRepository, LyricsRepository>();

        services.AddHostedService<LiteDbInitializer>();

        services.AddSingleton<IKeyedLocker, KeyedLocker>();

        services.AddHealthChecks()
            .AddCheck<LiteDbHealthCheck>("litedb", tags: ["ready"]);

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddExternalHttpClient<IMusicService, LastFmService, LastFmSettings>(
            configuration, "lastfm", "Services:LastFM",
            client => client.DefaultRequestHeaders.TryAddWithoutValidation("Content-type", "application/json"));

        services.AddExternalHttpClient<IMusicService, MusicBrainzService, MusicBrainzSettings>(
            configuration, "musicbrainz", "Services:MusicBrainz");

        services.AddExternalHttpClient<IMusicService, FanartService, FanartSettings>(
            configuration, "fanart", "Services:Fanart");

        services.AddExternalHttpClient<IMusicService, CoverArtService, CoverArtSettings>(
            configuration, "coverart", "Services:CoverArt");

        services.AddExternalHttpClient<ILyricsService, LyricsOvhService, LyricsOvhSettings>(
            configuration, "lyricsovh", "Services:LyricsOvh");

        services.AddExternalHttpClient<ILyricsService, LrcLibService, LrcLibSettings>(
            configuration, "lrclib", "Services:LrcLib");

        services.AddSingleton<IMusicBrainzLookup>(sp =>
            sp.GetRequiredService<IEnumerable<IMusicService>>().OfType<MusicBrainzService>().Single());

        services.AddSingleton<IMusicAggregator, MusicAggregator>();
        services.AddSingleton<ILyricsAggregator, LyricsAggregator>();

        services.Configure<RateLimitOptions>(configuration.GetSection("ServiceRateLimits"));

        return services;
    }

    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        string? otlpEndpoint = configuration["Telemetry:OTEL_EXPORTER_OTLP_ENDPOINT"];
        string? newRelicInsertKey = configuration["Telemetry:NEW_RELIC_INSERT_KEY"];
        bool hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);

        OpenTelemetryBuilder otel = services.AddOpenTelemetry()
           .WithTracing(tracingBuilder =>
           {
               tracingBuilder
                   .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MusicDataApi"))
                   .AddSource("MusicDataApi")
                   .AddAspNetCoreInstrumentation()
                   .AddHttpClientInstrumentation();

               if (hasOtlpEndpoint)
               {
                   tracingBuilder.AddOtlpExporter(options =>
                   {
                       options.Endpoint = new Uri(otlpEndpoint! + "/v1/traces");
                       options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                       if (!string.IsNullOrEmpty(newRelicInsertKey))
                           options.Headers = $"api-key={newRelicInsertKey}";
                   });
               }
           })
           .WithMetrics(metricsBuilder =>
           {
               metricsBuilder
                   .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MusicDataApi"))
                   .AddMeter("MusicDataApi")
                   .AddAspNetCoreInstrumentation()
                   .AddHttpClientInstrumentation();

               if (hasOtlpEndpoint)
               {
                   metricsBuilder.AddOtlpExporter(options =>
                   {
                       options.Endpoint = new Uri(otlpEndpoint! + "/v1/metrics");
                       options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                       if (!string.IsNullOrEmpty(newRelicInsertKey))
                           options.Headers = $"api-key={newRelicInsertKey}";
                   });
               }
           });

        return services;
    }

    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "ApiKeyScheme";
            options.DefaultChallengeScheme = "ApiKeyScheme";
        })
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                "ApiKeyScheme", options => { options.TimeProvider = TimeProvider.System; });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiKeyPolicy", policy =>
            {
                policy.AddAuthenticationSchemes("ApiKeyScheme");
                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }

    public static IServiceCollection AddIpRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        int requestsPerMinute = configuration.GetValue<int?>("RateLimiting:RequestsPerMinute") ?? 60;

        services.AddRateLimiter(options =>
        {
            options.AddPolicy("IpPolicy", httpContext =>
            {
                string clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
                                  ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                                  ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = requestsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "text/plain";
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.Response.WriteAsync("Too many requests. Try again later.", ct);
            };
        });

        return services;
    }

    public static WebApplication UseIpRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
        return app;
    }
}
