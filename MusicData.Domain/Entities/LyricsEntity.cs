namespace MusicData.Domain.Entities;

public class LyricsEntity
{
    public int Id { get; set; }

    public DateTime UpdateDateTime { get; set; }

    public int Version { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string? PlainLyrics { get; set; }

    public string? SyncLyrics { get; set; }
}