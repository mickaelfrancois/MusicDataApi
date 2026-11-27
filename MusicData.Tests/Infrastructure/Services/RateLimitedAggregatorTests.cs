using Microsoft.Extensions.Options;
using MusicData.Application.Exceptions;
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
    public async Task RunAllSafeAsync_CountsFailures_ButNotGenuineNotFound()
    {
        TestAggregator sut = new([new FakeService(_ => throw new HttpRequestException("503")), new FakeService(_ => null), new FakeService(_ => "ok")]);

        var run = await sut.RunDetailedAsync();

        Assert.Equal(1, run.Failures);
        Assert.Single(run.Items);
        Assert.False(run.Throws);
    }

    [Fact]
    public async Task RunAllSafeAsync_AllFailedAndNothingFound_IsUnavailable_NotNotFound()
    {
        TestAggregator sut = new([new FakeService(_ => throw new TimeoutException()), new FakeService(_ => null)]);

        var run = await sut.RunDetailedAsync();

        Assert.Equal(1, run.Failures);
        Assert.Empty(run.Items);
        Assert.True(run.Throws);
    }

    [Fact]
    public async Task RunAllSafeAsync_NoFailuresAndNothingFound_IsPlainNotFound()
    {
        TestAggregator sut = new([new FakeService(_ => null)]);

        var run = await sut.RunDetailedAsync();

        Assert.Empty(run.Items);
        Assert.False(run.Throws);
    }

    [Fact]
    public async Task RunAllSafeAsync_CallerCancellation_Propagates()
    {
        using CancellationTokenSource cts = new();
        TestAggregator sut = new([new FakeService(ct => { cts.Cancel(); ct.ThrowIfCancellationRequested(); return "x"; })]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunDetailedAsync(cts.Token));
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

        public async Task<IReadOnlyList<string>> RunAsync()
            => (await RunAllSafeAsync<string>("test", (s, ct) => s.ProduceAsync(ct), CancellationToken.None)).Items;

        public async Task<(IReadOnlyList<string> Items, int Failures, bool Throws)> RunDetailedAsync(CancellationToken token = default)
        {
            RunResult<string> run = await RunAllSafeAsync<string>("test", (s, ct) => s.ProduceAsync(ct), token);

            bool throws = false;
            try { run.RequireItemsOrNotFound("test"); }
            catch (ExternalServicesUnavailableException) { throws = true; }

            return (run.Items, run.Failures, throws);
        }
    }
}
