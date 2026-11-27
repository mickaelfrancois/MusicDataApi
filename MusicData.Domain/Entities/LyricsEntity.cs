namespace MusicData.Domain.Entities;

public class LyricsEntity : IVersionedEntity
{
    public int Id { get; set; }

    public DateTime UpdateDateTime { get; set; }

    public int Version { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string AlbumName { get; set; } = string.Empty;

    public int Duration { get; set; }

    public string? PlainLyrics { get; set; }

    public string? SyncLyrics { get; set; }

    /// <summary>
    /// True for a negative-cache marker: no provider had lyrics for this key.
    /// Such a row must be treated as "known missing", never returned as lyrics.
    /// </summary>
    public bool NotFound { get; set; }
}
