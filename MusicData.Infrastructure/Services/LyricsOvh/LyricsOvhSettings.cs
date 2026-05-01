namespace MusicData.Infrastructure.Services.LyricsOvh;

public class LyricsOvhSettings : IHttpServiceSettings
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}