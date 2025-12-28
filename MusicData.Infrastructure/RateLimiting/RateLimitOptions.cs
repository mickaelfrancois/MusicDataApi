namespace MusicData.Infrastructure.RateLimiting;

public class RateLimitOptions
{
    public Dictionary<string, (int MaxRequests, int PerMilliSeconds)> ServiceLimits { get; set; } = [];
}
