using LiteDB;
using MusicData.Application.Interfaces;
using MusicData.Domain.Entities;

namespace MusicData.Infrastructure.Repositories;

internal sealed class ArtistRepository : IArtistRepository
{
    private readonly ILiteCollection<ArtistEntity> _collection;
    private const string CollectionName = "artists";

    public ArtistRepository(ILiteDatabase database)
    {
        _collection = database.GetCollection<ArtistEntity>(CollectionName);
        _collection.EnsureIndex(x => x.Name, unique: false);
        _collection.EnsureIndex(x => x.MusicBrainzID, unique: true);
    }


    public void Add(ArtistEntity artist)
    {
        ArtistEntity? existing = !string.IsNullOrWhiteSpace(artist.MusicBrainzID)
            ? FindByMusicBrainzID(artist.MusicBrainzID)
            : FindByName(artist.Name);

        if (existing is not null)
        {
            artist.UpdateDateTime = DateTime.UtcNow;
            artist.Version = 1;
            _collection.Update(existing.Id, artist);
        }
        else
        {
            artist.UpdateDateTime = DateTime.UtcNow;
            artist.Version = 1;
            _collection.Insert(artist);
        }
    }


    public ArtistEntity? GetByMusicBrainzID(string musicBrainzID) => FindByMusicBrainzID(musicBrainzID);


    public ArtistEntity? GetByName(string name) => FindByName(name);


    private ArtistEntity? FindByMusicBrainzID(string musicBrainzID) =>
        _collection.FindOne("LOWER($.MusicBrainzID) = @0", (musicBrainzID ?? string.Empty).ToLowerInvariant());


    private ArtistEntity? FindByName(string name) =>
        _collection.FindOne("LOWER($.Name) = @0", (name ?? string.Empty).ToLowerInvariant());
}
