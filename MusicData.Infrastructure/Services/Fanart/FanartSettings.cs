namespace MusicData.Infrastructure.Services.Fanart;

public class FanartSettings : IHttpServiceSettings
{
    public bool Enabled { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}