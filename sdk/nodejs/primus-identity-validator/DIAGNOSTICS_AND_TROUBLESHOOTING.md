# Diagnostics & Troubleshooting (Node.js)

This mirrors the NuGet guidance for investigating validation issues.

## Enable verbose logs
- Set `DEBUG=axios` to inspect JWKS/metadata fetches.
- Temporarily set `clockSkew` higher in dev to rule out clock drift.

## Common errors
- **"HTTPS is required for authority"**: set `requireHttpsMetadata = false` only in local dev.
- **"Untrusted issuer"**: ensure the token `iss` matches an entry in `issuers`.
- **"jwt audience invalid"**: audience must match one of `audiences`.
- **Rate limited (429)**: repeated failures exceeded `rateLimiting.maxFailuresPerWindow`.

## JWKS diagnostics
- JWKS cached for `jwksCacheTtl` hours.
- If rotating keys, set a smaller `jwksCacheTtl` and restart the service after rotation.

## Tenant resolution failures
- If `tenantResolver` throws, the request returns 401.
- Log inside the resolver sparingly; avoid async network calls in the hot path.
