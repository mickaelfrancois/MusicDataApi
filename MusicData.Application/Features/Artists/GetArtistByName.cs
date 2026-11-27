using Microsoft.Extensions.Logging;
using MusicData.Application.DTOs;
using MusicData.Application.Features.Common;
using MusicData.Application.Interfaces;
using MusicData.Application.Mappers;
using MusicData.Domain.Entities;
using MusicData.Shared;

namespace MusicData.Application.Features.Artists;

public interface IGetArtistByName
{
    Task<ArtistDto?> HandleAsync(string artistName, CancellationToken cancellationToken = default);
}

public sealed class GetArtistByName(
    IArtistRepository repository,
    IMusicAggregator aggregator,
    IKeyedLocker locker,
    ILogger<GetArtistByName> logger)
    : CachedReadHandler<string, ArtistDto, ArtistEntity>(locker, logger), IGetArtistByName
{
    protected override string EntityKind => "artist";

    protected override bool IsValid(string artistName) => !string.IsNullOrWhiteSpace(artistName);

    protected override ArtistEntity? GetCachedEntity(string artistName) => repository.GetByName(artistName);

    protected override Task<ArtistDto?> FetchAsync(string artistName, CancellationToken cancellationToken)
        => aggregator.GetArtistByNameAsync(artistName, cancellationToken);

    protected override ArtistDto MapToDto(ArtistEntity entity) => entity.ToDto();

    protected override void Persist(string artistName, ArtistDto dto)
    {
        ArtistEntity entity = dto.ToEntity();

        // The artist is stored under its canonical (MusicBrainz) name; remember
        // the queried name too, or "Beatles" would never hit "The Beatles".
        if (NameAliases.ForQuery(artistName, entity.Name) is { } alias)
            entity.Aliases.Add(alias);

        repository.Add(entity);
    }

    protected override string MakeLockKey(string artistName) => $"artist:byname:{artistName.ToLowerInvariant()}";
}
