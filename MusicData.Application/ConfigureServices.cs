using Microsoft.Extensions.DependencyInjection;
using MusicData.Application.Features.Albums;
using MusicData.Application.Features.Artists;
using MusicData.Application.Features.Lyrics;

namespace MusicData.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddScoped<IGetArtistByName, GetArtistByName>();
        services.AddScoped<IGetArtistByMusicBrainzId, GetArtistByMusicBrainzId>();
        services.AddScoped<IGetAlbumByName, GetAlbumByName>();
        services.AddScoped<IGetAlbumByMusicBrainzId, GetAlbumByMusicBrainzId>();
        services.AddScoped<IGetLyrics, GetLyrics>();

        return services;
    }
}
