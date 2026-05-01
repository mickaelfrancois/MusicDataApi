using System.Net;

namespace MusicData.Tests.Api.E2E;

public class HealthTests : IClassFixture<MusicDataApiFactory>
{
    private readonly MusicDataApiFactory _factory;

    public HealthTests(MusicDataApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthLive_ReturnsOk_WithoutAuth()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_ReturnsOk_WhenLiteDbReachable()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Healthy\"", body);
    }
}
