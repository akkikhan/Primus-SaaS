# Diagnostics & Troubleshooting (OIDC/JWKS)

This guide covers the most common integration issues for OIDC/Azure AD with Primus Identity. Claims mapping is intentionally out-of-scope here.

## Quick Checks
- Authority: use the tenant root (no double `/v2.0`). Example: `https://login.microsoftonline.com/<tenant-id>`
- Issuer: should include `/v2.0` when using the v2 endpoint. Example: `https://login.microsoftonline.com/<tenant-id>/v2.0`
- JWKS: should come from discovery metadata, not manually concatenated.
- Audiences: must be non-empty and match the token’s `aud`.
- Unique identifiers: issuer names and issuer claim values must be unique across issuers.

## Configuration Validation
`PrimusIdentityOptions.Validate()` now aggregates errors. Examples:
- Missing authority for OIDC: `Authority URL is required for OIDC/AzureAD issuer 'AzureAD'.`
- Non-HTTPS authority: `Authority must use HTTPS for 'AzureAD'.`
- Duplicate issuer name: `Duplicate issuer name 'AzureAD'.`
- Duplicate issuer claim: `Duplicate issuer claim value 'https://.../v2.0'.`
- JWT missing secret/JWKS: `Secret or JWKS URL is required for JWT issuer 'Local'.`

## JWKS Fetch Resiliency
- JWKS fetch uses retry/backoff (`MaxRetries`, `BaseDelay` in `JwksServiceOptions`).
- Cache + diagnostics:
  - `GetDiagnostics()` provides cache hits/misses, fetch attempts/failures, and last success timestamp.
  - Use this to surface in health/diagnostics endpoints.
- Expose a diagnostics endpoint:
  ```csharp
  app.MapPrimusIdentityDiagnostics(); // GET /primus/diagnostics
  ```

## Common Failure Patterns
- **IDX10511 / 404 on JWKS**: Remove `/v2.0` from `Authority`; let discovery derive JWKS URI.
- **HTTP 500/timeout during JWKS fetch**: Increase `MaxRetries`/`BaseDelay`; verify outbound connectivity/firewalls.
- **Unknown issuer**: Ensure token `iss` matches one configured `Issuer` exactly (case-sensitive).
- **Audience validation failure**: Ensure at least one configured audience matches token `aud`.

## Suggested Logging
- Log authority, issuer, and discovered JWKS URI at startup (INFO).
- Log JWKS fetch retries at WARNING with attempt count and delay.
- Log validation failures with token issuer/audience and the attempted JWKS URI (no token contents).
- If rate limiting is enabled, log when limits are hit (429).

## Health & Monitoring
- Expose `JwksService.GetDiagnostics()` and cache counts in a diagnostics/health endpoint.
- Track:
  - CacheHits/CacheMisses
  - FetchAttempts/FetchFailures
  - LastSuccessUtc
- Alert on sustained fetch failures or zero cache hits (indicates cache bypass).

## Rate Limiting for Failed Validations
- Configure in `PrimusIdentityOptions.RateLimiting`:
  ```csharp
  options.RateLimiting = new FailedValidationRateLimiterOptions
  {
      Enabled = true,
      MaxFailuresPerWindow = 20,
      MaxGlobalFailuresPerWindow = 0, // disable global ceiling
      Window = TimeSpan.FromMinutes(1)
  };
  ```
- On repeated failed validations per client IP, responses return 429 with `Retry-After`.
- Keep limits conservative to avoid blocking legitimate users during outages.

## Minimal Working Azure AD Example
```csharp
services.AddPrimusIdentity(options =>
{
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.AzureAD,
            Authority = "https://login.microsoftonline.com/<tenant-id>",
            Issuer = "https://login.microsoftonline.com/<tenant-id>/v2.0",
            Audiences = new List<string> { "api://<client-id>" }
        }
    };
});
```
