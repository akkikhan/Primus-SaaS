using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrimusSaaS.Identity.Validator.Services;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// Simple header-based API key authentication handler.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IOptionsMonitor<PrimusIdentityOptions> _identityOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IOptionsMonitor<PrimusIdentityOptions> identityOptions) : base(options, logger, encoder, clock)
    {
        _identityOptions = identityOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var primusOptions = _identityOptions.CurrentValue;
        var apiKeyOptions = primusOptions.ApiKey;
        if (apiKeyOptions == null || !apiKeyOptions.Enabled)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var headerName = string.IsNullOrWhiteSpace(apiKeyOptions.HeaderName)
            ? ApiKeyDefaults.DefaultHeaderName
            : apiKeyOptions.HeaderName;

        if (!Request.Headers.TryGetValue(headerName, out var providedKey) || string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var entry = apiKeyOptions.Keys.FirstOrDefault(k => string.Equals(k.Key, providedKey, StringComparison.Ordinal));
        if (entry == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, entry.Name),
            new Claim(ClaimTypes.Name, entry.Name),
            new Claim("auth_type", "api_key")
        };

        if (!string.IsNullOrWhiteSpace(entry.TenantId))
        {
            claims.Add(new Claim("tid", entry.TenantId));
        }

        foreach (var role in entry.Roles ?? Enumerable.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        foreach (var kvp in entry.Claims ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value != null)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value));
            }
        }

        var identity = new ClaimsIdentity(claims, ApiKeyDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
