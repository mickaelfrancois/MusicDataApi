using Microsoft.Extensions.Options;
using MusicData.Infrastructure.RateLimiting;
using MusicData.Infrastructure.Services;

namespace MusicData.Tests.Infrastructure.Services;

public class RateLimitedAggregatorTests
{
    [Fact]
    public async Task RunAllSafeAsync_ReturnsNonNullResults_FromAllServices()
    {
        FakeService a = new(_ => "a");
        FakeService b = new(_ => "b");
        TestAggregator sut = new([a, b]);

        IReadOnlyList<string> results = await sut.RunAsync();

        Assert.Equal(2, results.Count);
        Assert.Contains("a", results);
        Assert.Contains("b", results);
    }

    [Fact]
    public async Task RunAllSafeAsync_FiltersOutNullResults()
    {
        FakeService ok = new(_ => "ok");
        FakeService missing = new(_ => null);
        TestAggregator sut = new([ok, missing]);

        IReadOnlyList<string> results = await sut.RunAsync();

        Assert.Single(results);
        Assert.Equal("ok", results[0]);
    }

    [Fact]
    public async Task RunAllSafeAsync_SwallowsExceptions_AndContinuesWithOthers()
    {
        FakeService boom = new(_ => throw new InvalidOperationException("boom"));
        FakeService quiet = new(_ => "quiet");
        TestAggregator sut = new([boom, quiet]);

        IReadOnlyList<string> results = await sut.RunAsync();

        Assert.Single(results);
        Assert.Equal("quiet", results[0]);
    }

    [Fact]
    public async Task RunAllSafeAsync_EmptyServices_ReturnsEmpty()
    {
        TestAggregator sut = new([]);

        IReadOnlyList<string> results = await sut.RunAsync();

        Assert.Empty(results);
    }


    private interface IFakeService
    {
        Task<string?> ProduceAsync(CancellationToken ct);
    }

    private sealed class FakeService(Func<CancellationToken, string?> body) : IFakeService
    {
        public Task<string?> ProduceAsync(CancellationToken ct) => Task.FromResult(body(ct));
    }

    private sealed class TestAggregator : RateLimitedAggregator<IFakeService>
    {
        public TestAggregator(IEnumerable<IFakeService> services)
            : base(services, Options.Create(new RateLimitOptions()))
        {
        }

        public Task<IReadOnlyList<string>> RunAsync()
            => RunAllSafeAsync<string>("test", (s, ct) => s.ProduceAsync(ct), CancellationToken.None);
    }
}
