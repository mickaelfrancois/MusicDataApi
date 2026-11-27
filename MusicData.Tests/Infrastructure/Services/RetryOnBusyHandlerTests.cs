using System.Net;
using MusicData.Infrastructure.Services;

namespace MusicData.Tests.Infrastructure.Services;

public class RetryOnBusyHandlerTests
{
    [Fact]
    public async Task BusyThenOk_RetriesAndReturnsTheSuccess()
    {
        (HttpClient client, Script script, List<TimeSpan> waits) = Create(2, HttpStatusCode.ServiceUnavailable, HttpStatusCode.TooManyRequests, HttpStatusCode.OK);

        HttpResponseMessage response = await client.GetAsync("https://mb.test/x");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, script.Calls);
        Assert.Equal([RetryOnBusyHandler.BaseDelay, RetryOnBusyHandler.BaseDelay * 2], waits);
    }

    [Fact]
    public async Task StillBusyAfterMaxRetries_ReturnsTheLastBusyResponse()
    {
        (HttpClient client, Script script, _) = Create(2, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

        HttpResponseMessage response = await client.GetAsync("https://mb.test/x");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(3, script.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task OtherStatuses_AreNotRetried(HttpStatusCode status)
    {
        (HttpClient client, Script script, _) = Create(2, status, HttpStatusCode.OK);

        HttpResponseMessage response = await client.GetAsync("https://mb.test/x");

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(1, script.Calls);
    }

    [Fact]
    public async Task ShortRetryAfter_NeverShortensTheBackoff()
    {
        (HttpClient client, Script script, List<TimeSpan> waits) = Create(2, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        script.RetryAfterSeconds.AddRange([1, 1]);

        await client.GetAsync("https://mb.test/x");

        Assert.Equal([RetryOnBusyHandler.BaseDelay, RetryOnBusyHandler.BaseDelay * 2], waits);
    }

    [Fact]
    public async Task RetryAfter_IsHonoured_ButCapped()
    {
        (HttpClient client, Script script, List<TimeSpan> waits) = Create(2, HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        script.RetryAfterSeconds.AddRange([2, 600]);

        await client.GetAsync("https://mb.test/x");

        Assert.Equal([TimeSpan.FromSeconds(2), RetryOnBusyHandler.MaxDelay], waits);
    }

    [Fact]
    public async Task NonGet_IsNotRetried()
    {
        (HttpClient client, Script script, _) = Create(2, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

        HttpResponseMessage response = await client.PostAsync("https://mb.test/x", new StringContent(""));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, script.Calls);
    }


    private static (HttpClient, Script, List<TimeSpan>) Create(int maxRetries, params HttpStatusCode[] statuses)
    {
        Script script = new(statuses);
        List<TimeSpan> waits = [];
        RetryOnBusyHandler handler = new(maxRetries, (wait, _) => { waits.Add(wait); return Task.CompletedTask; }) { InnerHandler = script };
        return (new HttpClient(handler), script, waits);
    }

    private sealed class Script(HttpStatusCode[] statuses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public List<int> RetryAfterSeconds { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(statuses[Calls]);
            if (Calls < RetryAfterSeconds.Count)
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(RetryAfterSeconds[Calls]));
            Calls++;
            return Task.FromResult(response);
        }
    }
}
