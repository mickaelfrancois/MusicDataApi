using LiteDB;
using Microsoft.Extensions.Hosting;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

/// <summary>
/// Runs the EnsureIndex calls for every LiteDB collection once at process
/// startup, so individual scoped repositories don't have to re-issue them on
/// every request.
/// </summary>
internal sealed class LiteDbInitializer(ILiteDatabase database) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        ArtistRepository.EnsureIndexes(database.GetCollection<ArtistEntity>(ArtistRepository.CollectionName));
        AlbumRepository.EnsureIndexes(database.GetCollection<AlbumEntity>(AlbumRepository.CollectionName));
        LyricsRepository.EnsureIndexes(database.GetCollection<LyricsEntity>(LyricsRepository.CollectionName));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
