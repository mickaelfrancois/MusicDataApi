using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Common;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Albums;

public interface IGetAlbumByMusicBrainzId
{
    Task<AlbumDto?> HandleAsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken = default);
}

public sealed record AlbumByMbidKey(string AlbumMusicBrainzId, string ArtistMusicBrainzId);

public sealed class GetAlbumByMusicBrainzId(
    IAlbumRepository repository,
    IMusicAggregator aggregator,
    IKeyedLocker locker,
    ILogger<GetAlbumByMusicBrainzId> logger)
    : CachedReadHandler<AlbumByMbidKey, AlbumDto, AlbumEntity>(locker, logger), IGetAlbumByMusicBrainzId
{
    public Task<AlbumDto?> HandleAsync(string albumMusicBrainzId, string artistMusicBrainzId, CancellationToken cancellationToken = default)
        => HandleAsync(new AlbumByMbidKey(albumMusicBrainzId, artistMusicBrainzId), cancellationToken);

    protected override string EntityKind => "album";

    protected override bool IsValid(AlbumByMbidKey key)
        => !string.IsNullOrWhiteSpace(key.AlbumMusicBrainzId) && !string.IsNullOrWhiteSpace(key.ArtistMusicBrainzId);

    protected override AlbumEntity? GetCachedEntity(AlbumByMbidKey key)
        => repository.GetByMusicBrainzID(key.AlbumMusicBrainzId);

    protected override Task<AlbumDto?> FetchAsync(AlbumByMbidKey key, CancellationToken cancellationToken)
        => aggregator.GetAlbumByMusicBrainzIdAsync(key.AlbumMusicBrainzId, key.ArtistMusicBrainzId, cancellationToken);

    protected override AlbumDto MapToDto(AlbumEntity entity) => entity.ToDto();

    protected override void Persist(AlbumDto dto) => repository.Add(dto.ToEntity());

    protected override string MakeLockKey(AlbumByMbidKey key) => $"album:bymbid:{key.AlbumMusicBrainzId.ToLowerInvariant()}";
}
