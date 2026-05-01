using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Common;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;

namespace MusicData.Application.Features.Artists;

public interface IGetArtistByMusicBrainzId
{
    Task<ArtistDto?> HandleAsync(string musicBrainzId, CancellationToken cancellationToken = default);
}

public sealed class GetArtistByMusicBrainzId(
    IArtistRepository repository,
    IMusicAggregator aggregator,
    IKeyedLocker locker,
    ILogger<GetArtistByMusicBrainzId> logger)
    : CachedReadHandler<string, ArtistDto, ArtistEntity>(locker, logger), IGetArtistByMusicBrainzId
{
    protected override string EntityKind => "artist";

    protected override bool IsValid(string musicBrainzId) => !string.IsNullOrWhiteSpace(musicBrainzId);

    protected override ArtistEntity? GetCachedEntity(string musicBrainzId) => repository.GetByMusicBrainzID(musicBrainzId);

    protected override Task<ArtistDto?> FetchAsync(string musicBrainzId, CancellationToken cancellationToken)
        => aggregator.GetArtistByMusicBrainzIdAsync(musicBrainzId, cancellationToken);

    protected override ArtistDto MapToDto(ArtistEntity entity) => entity.ToDto();

    protected override void Persist(ArtistDto dto) => repository.Add(dto.ToEntity());

    protected override string MakeLockKey(string musicBrainzId) => $"artist:bymbid:{musicBrainzId.ToLowerInvariant()}";
}
