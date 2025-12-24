namespace MusicData.Infrastructure.Services.LrcLib;

public class LrcLibSettings
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}