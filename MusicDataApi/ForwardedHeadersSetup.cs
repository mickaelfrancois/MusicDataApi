using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace MusicData.Api;

public static class ForwardedHeadersSetup
{
    /// <summary>
    /// X-Forwarded-* is only honoured from trusted hops. By default ASP.NET
    /// trusts loopback only, so behind a Docker/reverse proxy the per-IP rate
    /// limiter would see the proxy's address for every client. List the proxy
    /// in <c>ForwardedHeaders:KnownProxies</c> (IPs) or
    /// <c>ForwardedHeaders:KnownNetworks</c> (CIDR, e.g. "172.18.0.0/16").
    /// </summary>
    public static ForwardedHeadersOptions Create(IConfiguration configuration)
    {
        ForwardedHeadersOptions options = new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        foreach (string proxy in configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
            options.KnownProxies.Add(IPAddress.Parse(proxy));

        foreach (string network in configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));

        return options;
    }
}
