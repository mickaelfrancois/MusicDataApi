using LiteDB;
using MusicData.Domain.Entities;
using MusicData.Infrastructure.Repositories;

namespace MusicData.Tests.Infrastructure.Repositories;

public class LiteDbRepositoryRaceTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");

    public LiteDbRepositoryRaceTests()
    {
        ArtistRepository.EnsureIndexes(_db.GetCollection<ArtistEntity>(ArtistRepository.CollectionName));
    }

    public void Dispose() => _db.Dispose();


    [Fact]
    public void Add_WhenAConcurrentInsertWinsTheRace_UpdatesInsteadOfThrowing()
    {
        new ArtistRepository(_db).Add(new ArtistEntity { Name = "Foo", MusicBrainzID = "mbid-1", Biography = "winner" });

        // Simulates the stale read: the first lookup misses the row that the
        // "other request" has just inserted, so Insert hits the unique index.
        StaleReadRepository sut = new(_db);
        sut.Add(new ArtistEntity { Name = "Foo", MusicBrainzID = "mbid-1", Biography = "loser" });

        ILiteCollection<ArtistEntity> col = _db.GetCollection<ArtistEntity>(ArtistRepository.CollectionName);
        Assert.Equal(1, col.Count());
        Assert.Equal("loser", col.FindAll().Single().Biography);
    }


    private sealed class StaleReadRepository(ILiteDatabase db) : LiteDbRepository<ArtistEntity>(db, ArtistRepository.CollectionName)
    {
        private bool _firstCall = true;

        protected override ArtistEntity? FindExisting(ArtistEntity incoming)
        {
            if (_firstCall)
            {
                _firstCall = false;
                return null;
            }

            return FindByLowerField(nameof(ArtistEntity.MusicBrainzID), incoming.MusicBrainzID);
        }
    }
}
