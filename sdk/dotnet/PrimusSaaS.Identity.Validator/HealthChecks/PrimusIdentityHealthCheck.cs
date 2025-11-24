using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace PrimusSaaS.Identity.Validator.HealthChecks;

/// <summary>
/// Health check that verifies connectivity to configured identity providers.
/// </summary>
public class PrimusIdentityHealthCheck : IHealthCheck
{
    private readonly PrimusIdentityOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public PrimusIdentityHealthCheck(IOptions<PrimusIdentityOptions> options, IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        foreach (var issuer in _options.Issuers)
        {
            // Only check remote endpoints
            if (issuer.Type.IsOidcBased() && !string.IsNullOrEmpty(issuer.Authority))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    // Timeout quickly for health checks
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    var discoveryUrl = $"{issuer.Authority.TrimEnd('/')}/.well-known/openid-configuration";
                    var response = await client.GetAsync(discoveryUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        errors.Add($"OIDC Issuer '{issuer.Name}' unreachable: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"OIDC Issuer '{issuer.Name}' check failed: {ex.Message}");
                }
            }
            else if (issuer.Type == IssuerType.Jwt && !string.IsNullOrEmpty(issuer.JwksUrl))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(5);

                    var response = await client.GetAsync(issuer.JwksUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        errors.Add($"JWT Issuer '{issuer.Name}' JWKS unreachable: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"JWT Issuer '{issuer.Name}' JWKS check failed: {ex.Message}");
                }
            }
        }

        if (errors.Any())
        {
            return HealthCheckResult.Degraded($"Identity issues: {string.Join("; ", errors)}");
        }

        return HealthCheckResult.Healthy("All identity providers reachable");
    }
}
