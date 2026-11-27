using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using MusicData.Api;

namespace MusicData.Tests.Api;

public class ForwardedHeadersSetupTests
{
    [Fact]
    public void NothingConfigured_KeepsFrameworkDefaults_LoopbackOnly()
    {
        ForwardedHeadersOptions options = ForwardedHeadersSetup.Create(new ConfigurationBuilder().Build());

        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.Equal(2, options.KnownProxies.Count + options.KnownIPNetworks.Count);
    }

    [Fact]
    public void ConfiguredProxiesAndNetworks_AreTrusted()
    {
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownProxies:0"] = "10.1.2.3",
            ["ForwardedHeaders:KnownNetworks:0"] = "172.18.0.0/16",
        }).Build();

        ForwardedHeadersOptions options = ForwardedHeadersSetup.Create(config);

        Assert.Contains(IPAddress.Parse("10.1.2.3"), options.KnownProxies);
        Assert.Contains(options.KnownIPNetworks, n => n.Contains(IPAddress.Parse("172.18.5.9")));
    }
}
