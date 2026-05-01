using System.Net;

namespace MusicData.Tests.Api.E2E;

public class AuthTests : IClassFixture<MusicDataApiFactory>
{
    private readonly MusicDataApiFactory _factory;

    public AuthTests(MusicDataApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NoApiKey_Returns401()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/v1/artists/byName/Foo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongApiKey_Returns401()
    {
        using HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "definitely-not-the-key");

        HttpResponseMessage response = await client.GetAsync("/v1/artists/byName/Foo");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidApiKey_PassesAuth_AndReachesHandler()
    {
        // Aggregator returns null -> handler returns 404. The fact that we
        // get 404 (not 401) proves the X-Api-Key gate let us through.
        using HttpClient client = _factory.CreateAuthenticatedClient();

        HttpResponseMessage response = await client.GetAsync("/v1/artists/byName/Unknown");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
