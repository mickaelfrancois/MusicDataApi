namespace MusicData.Infrastructure.Services.MusicBrainz;

public class MusicBrainzSettings
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://musicbrainz.org/ws/2/";

    public int TimeoutSeconds { get; set; } = 10;
}