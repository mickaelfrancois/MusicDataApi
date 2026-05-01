namespace MusicData.Infrastructure.Services.MusicBrainz;

public interface IMusicBrainzLookup
{
    Task<string?> FindArtistAsync(string name, CancellationToken cancellationToken);

    Task<MusicBrainzReleaseInfo?> FindAlbumAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken);
}
