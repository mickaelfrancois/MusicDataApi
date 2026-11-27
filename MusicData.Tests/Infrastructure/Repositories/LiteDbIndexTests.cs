using LiteDB;
using MusicData.Domain.Entities;
using MusicData.Infrastructure.Repositories;

namespace MusicData.Tests.Infrastructure.Repositories;

/// <summary>
/// The repositories compare LOWER(field); a plain index on the field is not
/// used for that and every lookup was a full collection scan.
/// </summary>
public class LiteDbIndexTests : IDisposable
{
    private readonly LiteDatabase _db = new(":memory:");

    public void Dispose() => _db.Dispose();


    [Theory]
    [InlineData(ArtistRepository.ByNameQuery, "idx_name_lower")]
    [InlineData("LOWER($.MusicBrainzID) = @0", "idx_mbid_lower")]
    [InlineData(ArtistRepository.ByAliasQuery, ArtistRepository.AliasIndexName)]
    public void ArtistLookups_UseAnIndexSeek(string query, string expectedIndex)
    {
        ILiteCollection<ArtistEntity> collection = _db.GetCollection<ArtistEntity>(ArtistRepository.CollectionName);
        ArtistRepository.EnsureIndexes(collection);

        AssertIndexSeek(collection.Query().Where(query, "x").GetPlan(), expectedIndex);
    }

    [Theory]
    [InlineData(AlbumRepository.ByNameQuery, "idx_name_lower")]
    [InlineData("LOWER($.MusicBrainzID) = @0", "idx_mbid_lower")]
    [InlineData(AlbumRepository.ByAliasQuery, AlbumRepository.AliasIndexName)]
    public void AlbumLookups_UseAnIndexSeek(string query, string expectedIndex)
    {
        ILiteCollection<AlbumEntity> collection = _db.GetCollection<AlbumEntity>(AlbumRepository.CollectionName);
        AlbumRepository.EnsureIndexes(collection);

        AssertIndexSeek(collection.Query().Where(query, "x", "y").GetPlan(), expectedIndex);
    }

    [Fact]
    public void LyricsLookup_UsesAnIndexSeek()
    {
        ILiteCollection<LyricsEntity> collection = _db.GetCollection<LyricsEntity>(LyricsRepository.CollectionName);
        LyricsRepository.EnsureIndexes(collection);

        AssertIndexSeek(collection.Query().Where(LyricsRepository.ByTitleAndArtistQuery, "x", "y").GetPlan(), "idx_title_lower");
    }

    [Fact]
    public void EnsureIndexes_OnAnExistingDatabase_IndexesItsRows_AndDropsTheUnusedPlainIndexes()
    {
        // A database created by the previous version: plain indexes, existing rows.
        ILiteCollection<ArtistEntity> collection = _db.GetCollection<ArtistEntity>(ArtistRepository.CollectionName);
        collection.EnsureIndex(x => x.Name, unique: false);
        collection.EnsureIndex(x => x.MusicBrainzID, unique: true);
        collection.Insert(new ArtistEntity { Name = "Foo Fighters", MusicBrainzID = "MBID-1", Version = 2, UpdateDateTime = DateTime.UtcNow });

        ArtistRepository.EnsureIndexes(collection);
        ArtistRepository.EnsureIndexes(collection);

        ArtistRepository sut = new(_db);
        Assert.NotNull(sut.GetByName("foo fighters"));
        Assert.NotNull(sut.GetByMusicBrainzID("mbid-1"));

        List<string> indexes = _db.GetCollection("$indexes").FindAll().Select(i => i["name"].AsString).ToList();
        Assert.DoesNotContain("Name", indexes);
        Assert.Contains("MusicBrainzID", indexes);
        Assert.Contains("idx_name_lower", indexes);

        // The unique MBID constraint is still enforced.
        Assert.Throws<LiteException>(() => collection.Insert(new ArtistEntity { Name = "Dup", MusicBrainzID = "MBID-1" }));
    }


    private static void AssertIndexSeek(BsonDocument plan, string expectedIndex)
    {
        BsonDocument index = plan["index"].AsDocument;

        Assert.Equal(expectedIndex, index["name"].AsString);
        Assert.StartsWith("INDEX SEEK", index["mode"].AsString);
    }
}
