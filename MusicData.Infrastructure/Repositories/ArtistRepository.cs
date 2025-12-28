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
        _collection.EnsureIndex(x => x.Name, unique: true);
        _collection.EnsureIndex(x => x.MusicBrainzID, unique: true);
    }


    public void Add(ArtistEntity artist)
    {
        ArtistEntity? existing = null;

        if (!string.IsNullOrWhiteSpace(artist.MusicBrainzID))
            existing = _collection.FindOne(c => c.MusicBrainzID != null && c.MusicBrainzID.Equals(artist.MusicBrainzID, StringComparison.InvariantCultureIgnoreCase));

        existing ??= _collection.FindOne(c => c.Name.Equals(artist.Name, StringComparison.InvariantCultureIgnoreCase));

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


    public void Delete(int id)
    {
        _collection.Delete(id);
    }


    public ArtistEntity? GetByMusicBrainzID(string musicBrainzID)
    {
        return _collection.FindOne(c => c.MusicBrainzID != null && c.MusicBrainzID.Equals(musicBrainzID, StringComparison.InvariantCultureIgnoreCase));
    }


    public ArtistEntity? GetByName(string name)
    {
        return _collection.FindOne(c => c.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }


    public void Update(ArtistEntity artist)
    {
        _collection.Update(artist);
    }
}