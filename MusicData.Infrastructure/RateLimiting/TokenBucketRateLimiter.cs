namespace MusicData.Infrastructure.RateLimiting;

internal sealed class TokenBucketRateLimiter
{
    private readonly int _capacity;
    private readonly double _refillTokensPerMilliSecond;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly object _sync = new();

    public TokenBucketRateLimiter(int maxRequests, TimeSpan per)
    {
        if (maxRequests <= 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
        if (per.TotalSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(per));

        _capacity = maxRequests;
        _refillTokensPerMilliSecond = maxRequests / per.TotalMilliseconds;
        _tokens = _capacity;
        _lastRefill = DateTime.UtcNow;
    }

    private void Refill()
    {
        DateTime now = DateTime.UtcNow;
        double seconds = (now - _lastRefill).TotalMilliseconds;
        if (seconds <= 0)
            return;

        _tokens = Math.Min(_capacity, _tokens + (seconds * _refillTokensPerMilliSecond));
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

                double needed = (1.0 - _tokens) / _refillTokensPerMilliSecond;

                delayMs = Math.Max(1, (int)Math.Ceiling(needed));
            }

            await Task.Delay(delayMs, cancellationToken);
        }
    }
}