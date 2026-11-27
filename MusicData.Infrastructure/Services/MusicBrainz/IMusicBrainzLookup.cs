namespace MusicData.Infrastructure.Services.MusicBrainz;

/// <summary>
/// Name → MBID resolution. Both methods return null only for a genuine
/// "no match" and let every failure (HTTP error, timeout, busy) propagate.
/// </summary>
public interface IMusicBrainzLookup
{
    Task<string?> FindArtistAsync(string name, CancellationToken cancellationToken);

    Task<MusicBrainzReleaseInfo?> FindAlbumAsync(string albumName, string artistMusicBrainzId, CancellationToken cancellationToken);
}
