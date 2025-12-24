using System.Net;
using System.Threading.RateLimiting;
using LiteDB;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MusicData.Application.Interfaces;
using MusicData.Infrastructure.RateLimiting;
using MusicData.Infrastructure.Repositories;
using MusicData.Infrastructure.Security;
using MusicData.Infrastructure.Services;
using MusicData.Infrastructure.Services.Fanart;
using MusicData.Infrastructure.Services.LastFm;
using MusicData.Infrastructure.Services.LrcLib;
using MusicData.Infrastructure.Services.LyricsOvh;
using MusicData.Infrastructure.Services.MusicBrainz;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MusicData.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection AddDataContext(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetConnectionString("DefaultConnection"));

        services.AddScoped<ILiteDatabase, LiteDatabase>(c =>
        {
            return new LiteDatabase(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<ILyricsRepository, LyricsRepository>();

        return services;
    }


    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LastFmSettings>(configuration.GetSection("Services:LastFM"));
        services.Configure<MusicBrainzSettings>(configuration.GetSection("Services:MusicBrainz"));
        services.Configure<FanartSettings>(configuration.GetSection("Services:Fanart"));
        services.Configure<LyricsOvhSettings>(configuration.GetSection("Services:LyricsOvh"));
        services.Configure<LrcLibSettings>(configuration.GetSection("Services:LrcLib"));

        services.AddHttpClient<IMusicService, LastFmService>("lastfm", (sp, client) =>
        {
            LastFmSettings settings = sp.GetRequiredService<IOptions<LastFmSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-type", "application/json");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("RoK/1.0 (rok@francois.ovh)");
        })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
            });


        services.AddHttpClient<IMusicService, MusicBrainzService>("musicbrainz", (sp, client) =>
        {
            MusicBrainzSettings settings = sp.GetRequiredService<IOptions<MusicBrainzSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("RoK/1.0 (rok@francois.ovh)");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        });


        services.AddHttpClient<IMusicService, FanartService>("fanart", (sp, client) =>
        {
            FanartSettings settings = sp.GetRequiredService<IOptions<FanartSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("RoK/1.0 (rok@francois.ovh)");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        });

        services.AddHttpClient<ILyricsService, LyricsOvhService>("lyricsOvh", (sp, client) =>
        {
            LyricsOvhSettings settings = sp.GetRequiredService<IOptions<LyricsOvhSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("RoK/1.0 (rok@francois.ovh)");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        });

        services.AddHttpClient<ILyricsService, LrcLibService>("lrclib", (sp, client) =>
        {
            LrcLibSettings settings = sp.GetRequiredService<IOptions<LrcLibSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("RoK/1.0 (rok@francois.ovh)");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
        });

        services.AddScoped<IMusicAggregator, MusicAggregator>();
        services.AddScoped<ILyricsAggregator, LyricsAggregator>();

        RateLimitOptions rl = new();
        rl.ServiceLimits["musicbrainzservice"] = (MaxRequests: 1, PerSeconds: 1);
        rl.ServiceLimits["lastfmservice"] = (MaxRequests: 4, PerSeconds: 1);
        rl.ServiceLimits["fanartservice"] = (MaxRequests: 10, PerSeconds: 1);
        rl.ServiceLimits["lyricsovhservice"] = (MaxRequests: 10, PerSeconds: 1);
        rl.ServiceLimits["lrclibservice"] = (MaxRequests: 10, PerSeconds: 1);
        services.Configure<RateLimitOptions>(o =>
        {
            o.ServiceLimits = rl.ServiceLimits;
        });

        return services;
    }


    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenTelemetry()
           .WithTracing(tracingBuilder =>
           {
               tracingBuilder
                   .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MusicDataApi"))
                   .AddSource("MusicDataApi")
                   .AddAspNetCoreInstrumentation()
                   .AddHttpClientInstrumentation()
                   .AddOtlpExporter(options =>
                   {
                       string endpoint = configuration["Telemetry:OTEL_EXPORTER_OTLP_ENDPOINT"]!;
                       options.Endpoint = new Uri(endpoint + "/v1/traces");
                       options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;

                       string newRelicInsertKey = configuration["Telemetry:NEW_RELIC_INSERT_KEY"]!;
                       if (!string.IsNullOrEmpty(newRelicInsertKey))
                           options.Headers = $"api-key={newRelicInsertKey}";
                   });

               //if (builder.Environment.IsDevelopment())
               //    tracingBuilder.AddConsoleExporter();
           })
           .WithMetrics(metricsBuilder =>
           {
               metricsBuilder
                   .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MusicDataApi"))
                   .AddAspNetCoreInstrumentation()
                   .AddHttpClientInstrumentation()
                   .AddOtlpExporter(options =>
                   {
                       string endpoint = configuration["Telemetry:OTEL_EXPORTER_OTLP_ENDPOINT"]!;
                       options.Endpoint = new Uri(endpoint + "/v1/metrics");
                       options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;

                       string newRelicInsertKey = configuration["Telemetry:NEW_RELIC_INSERT_KEY"]!;
                       if (!string.IsNullOrEmpty(newRelicInsertKey))
                           options.Headers = $"api-key={newRelicInsertKey}";
                   });

               //if (builder.Environment.IsDevelopment())
               //    metricsBuilder.AddConsoleExporter();
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

        services.AddAuthorization();

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

                return RateLimitPartition.GetTokenBucketLimiter(clientIp, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = requestsPerMinute,
                    TokensPerPeriod = requestsPerMinute,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    AutoReplenishment = true
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
