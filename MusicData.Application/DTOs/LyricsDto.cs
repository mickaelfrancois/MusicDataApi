namespace MusicData.Application.DTOs;

public class LyricsDto : IOriginAware
{
    public string Origin { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsPartial { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ArtistName { get; set; } = string.Empty;

    public string AlbumName { get; set; } = string.Empty;

    public string? PlainLyrics { get; set; }

    public string? SyncLyrics { get; set; }

    public int Duration { get; set; }
}
