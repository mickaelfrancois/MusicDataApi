namespace MusicData.Infrastructure.Services;

public interface IHttpServiceSettings
{
    string BaseUrl { get; }

    int TimeoutSeconds { get; }

    bool Enabled { get; }
}
