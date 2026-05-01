namespace MusicData.Infrastructure.RateLimiting;

public class RateLimitOptions
{
    public Dictionary<string, ServiceRateLimit> ServiceLimits { get; set; } = [];
}

public class ServiceRateLimit
{
    public int MaxRequests { get; set; }

    public int PerMilliSeconds { get; set; }
}
