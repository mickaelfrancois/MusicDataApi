namespace MusicData.Infrastructure.RateLimiting;

internal sealed class TokenBucketRateLimiter
{
    private readonly int _capacity;
    private readonly double _refillTokensPerSecond;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly object _sync = new();

    public TokenBucketRateLimiter(int maxRequests, TimeSpan per)
    {
        if (maxRequests <= 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
        if (per.TotalSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(per));

        _capacity = maxRequests;
        _refillTokensPerSecond = maxRequests / per.TotalSeconds;
        _tokens = _capacity;
        _lastRefill = DateTime.UtcNow;
    }

    private void Refill()
    {
        DateTime now = DateTime.UtcNow;
        double seconds = (now - _lastRefill).TotalSeconds;
        if (seconds <= 0)
            return;

        _tokens = Math.Min(_capacity, _tokens + (seconds * _refillTokensPerSecond));
        _lastRefill = now;
    }

    public async Task WaitForAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            int delayMs;
            lock (_sync)
            {
                Refill();
                if (_tokens >= 1.0)
                {
                    _tokens -= 1.0;
                    return;
                }

                double needed = (1.0 - _tokens) / _refillTokensPerSecond;

                delayMs = Math.Max(1, (int)Math.Ceiling(needed * 1000.0));
            }

            await Task.Delay(delayMs, cancellationToken);
        }
    }
}