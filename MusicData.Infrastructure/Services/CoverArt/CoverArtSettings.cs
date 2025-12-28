namespace MusicData.Infrastructure.Services.CoverArt;

public class CoverArtSettings
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://coverartarchive.org";

    public int TimeoutSeconds { get; set; } = 10;
}
