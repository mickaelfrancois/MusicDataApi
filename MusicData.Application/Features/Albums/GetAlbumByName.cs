using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Common;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Albums;

public interface IGetAlbumByName
{
    Task<AlbumDto?> HandleAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken = default);
}

public sealed record AlbumByNameKey(string AlbumName, string ArtistMusicBrainzId);

public sealed class GetAlbumByName(
    IAlbumRepository repository,
    IMusicAggregator aggregator,
    IKeyedLocker locker,
    ILogger<GetAlbumByName> logger)
    : CachedReadHandler<AlbumByNameKey, AlbumDto, AlbumEntity>(locker, logger), IGetAlbumByName
{
    public Task<AlbumDto?> HandleAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken = default)
        => HandleAsync(new AlbumByNameKey(albumName, artistMusicBrainzId), cancellationToken);

    protected override string EntityKind => "album";

    protected override bool IsValid(AlbumByNameKey key)
        => !string.IsNullOrWhiteSpace(key.AlbumName) && !string.IsNullOrWhiteSpace(key.ArtistMusicBrainzId);

    protected override AlbumEntity? GetCachedEntity(AlbumByNameKey key)
        => repository.GetByName(key.AlbumName, key.ArtistMusicBrainzId);

    protected override Task<AlbumDto?> FetchAsync(AlbumByNameKey key, CancellationToken cancellationToken)
        => aggregator.GetAlbumByNameAsync(key.AlbumName, key.ArtistMusicBrainzId, cancellationToken);

    protected override AlbumDto MapToDto(AlbumEntity entity) => entity.ToDto();

    protected override void Persist(AlbumDto dto) => repository.Add(dto.ToEntity());

    protected override string MakeLockKey(AlbumByNameKey key)
        => $"album:byname:{key.ArtistMusicBrainzId.ToLowerInvariant()}:{key.AlbumName.ToLowerInvariant()}";
}
