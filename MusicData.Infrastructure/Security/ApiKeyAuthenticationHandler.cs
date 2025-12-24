using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace MusicData.Infrastructure.Security;

internal sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out StringValues headerValues))
            return Task.FromResult(AuthenticateResult.Fail("Missing API Key header."));

        string? providedApiKey = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));

        string? configuredApiKey = _configuration["ApiKeySettings:Key"];
        if (string.IsNullOrWhiteSpace(configuredApiKey))
            return Task.FromResult(AuthenticateResult.Fail("API Key not configured on server."));

        if (!IsApiKeyValid(providedApiKey, configuredApiKey))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));

        Claim[] claims = new[] { new Claim(ClaimTypes.NameIdentifier, "ApiKey") };
        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool IsApiKeyValid(string providedApiKey, string configuredApiKey)
    {
        bool keysMatch;

        try
        {
            byte[] providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedApiKey));
            byte[] configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredApiKey));

            keysMatch = CryptographicOperations.FixedTimeEquals(providedHash, configuredHash);
        }
        catch
        {
            keysMatch = false;
        }

        return keysMatch;
    }
}