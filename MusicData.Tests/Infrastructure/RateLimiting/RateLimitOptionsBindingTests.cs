using Microsoft.Extensions.Configuration;
using MusicData.Infrastructure.RateLimiting;

namespace MusicData.Tests.Infrastructure.RateLimiting;

public class RateLimitOptionsBindingTests
{
    [Fact]
    public void ServiceRateLimits_BindsFromJsonShape_UsedInAppsettings()
    {
        Dictionary<string, string?> inMemory = new()
        {
            ["ServiceRateLimits:ServiceLimits:musicbrainzservice:MaxRequests"] = "1",
            ["ServiceRateLimits:ServiceLimits:musicbrainzservice:PerMilliSeconds"] = "1200",
            ["ServiceRateLimits:ServiceLimits:lastfmservice:MaxRequests"] = "4",
            ["ServiceRateLimits:ServiceLimits:lastfmservice:PerMilliSeconds"] = "1000",
        };

        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

        RateLimitOptions options = new();
        config.GetSection("ServiceRateLimits").Bind(options);

        Assert.Equal(2, options.ServiceLimits.Count);
        Assert.Equal(1, options.ServiceLimits["musicbrainzservice"].MaxRequests);
        Assert.Equal(1200, options.ServiceLimits["musicbrainzservice"].PerMilliSeconds);
        Assert.Equal(4, options.ServiceLimits["lastfmservice"].MaxRequests);
        Assert.Equal(1000, options.ServiceLimits["lastfmservice"].PerMilliSeconds);
    }

    [Fact]
    public void ServiceRateLimits_MissingSection_LeavesDictionaryEmpty()
    {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

        RateLimitOptions options = new();
        config.GetSection("ServiceRateLimits").Bind(options);

        Assert.Empty(options.ServiceLimits);
    }
}
