namespace MusicData.Infrastructure.Services.LastFm;

public class LastFmSettings
{
    public bool Enabled { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://ws.audioscrobbler.com/2.0/";

    public int TimeoutSeconds { get; set; } = 10;
}