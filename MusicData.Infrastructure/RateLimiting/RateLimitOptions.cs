namespace MusicData.Infrastructure.RateLimiting;

public class RateLimitOptions
{
    public Dictionary<string, (int MaxRequests, int PerSeconds)> ServiceLimits { get; set; } = new();
}
