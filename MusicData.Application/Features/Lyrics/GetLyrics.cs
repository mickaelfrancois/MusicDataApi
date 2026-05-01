using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Common;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Lyrics;

public interface IGetLyrics
{
    Task<LyricsDto?> HandleAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken = default);
}

public sealed record LyricsKey(string Title, string ArtistName, string AlbumName, int Duration);

public sealed class GetLyrics(
    ILyricsRepository repository,
    ILyricsAggregator aggregator,
    IKeyedLocker locker,
    ILogger<GetLyrics> logger)
    : CachedReadHandler<LyricsKey, LyricsDto, LyricsEntity>(locker, logger), IGetLyrics
{
    public Task<LyricsDto?> HandleAsync(string title, string artistName, string albumName, int duration, CancellationToken cancellationToken = default)
        => HandleAsync(new LyricsKey(title, artistName, albumName, duration), cancellationToken);

    protected override string EntityKind => "lyrics";

    protected override bool IsValid(LyricsKey key)
        => !string.IsNullOrWhiteSpace(key.Title) && !string.IsNullOrEmpty(key.ArtistName);

    protected override LyricsEntity? GetCachedEntity(LyricsKey key) => repository.Get(key.Title, key.ArtistName);

    protected override Task<LyricsDto?> FetchAsync(LyricsKey key, CancellationToken cancellationToken)
        => aggregator.GetLyricsAsync(key.Title, key.ArtistName, key.AlbumName, key.Duration, cancellationToken);

    protected override LyricsDto MapToDto(LyricsEntity entity) => entity.ToDto();

    protected override void Persist(LyricsDto dto) => repository.Add(dto.ToEntity());

    protected override string MakeLockKey(LyricsKey key) => $"lyrics:{key.ArtistName.ToLowerInvariant()}:{key.Title.ToLowerInvariant()}";

    protected override void OnNotFound(LyricsKey key)
    {
        // Negative cache marker so we don't re-hit external services for known-missing lyrics.
        LyricsDto marker = new() { Title = key.Title, ArtistName = key.ArtistName, Origin = "NotFound" };
        Persist(marker);
    }
}
