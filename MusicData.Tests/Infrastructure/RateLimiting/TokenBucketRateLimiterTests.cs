using System.Diagnostics;
using MusicData.Infrastructure.RateLimiting;

namespace MusicData.Tests.Infrastructure.RateLimiting;

public class TokenBucketRateLimiterTests
{
    [Fact]
    public async Task BurstUpToCapacity_DoesNotWait()
    {
        TokenBucketRateLimiter limiter = new(maxRequests: 5, per: TimeSpan.FromSeconds(1));

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 5; i++)
            await limiter.WaitForAvailabilityAsync();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 100,
            $"Burst of 5 within capacity 5 should be near-instant, took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ExceedingCapacity_WaitsForRefill()
    {
        // 5 req / 1000ms => one token refills every 200ms.
        TokenBucketRateLimiter limiter = new(maxRequests: 5, per: TimeSpan.FromSeconds(1));

        for (int i = 0; i < 5; i++)
            await limiter.WaitForAvailabilityAsync();

        Stopwatch sw = Stopwatch.StartNew();
        await limiter.WaitForAvailabilityAsync();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 150,
            $"6th call should wait ~200ms for refill, waited {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"6th call should not wait excessively, waited {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Constructor_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenBucketRateLimiter(0, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenBucketRateLimiter(-1, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TokenBucketRateLimiter(1, TimeSpan.Zero));
        await Task.CompletedTask;
    }
}
