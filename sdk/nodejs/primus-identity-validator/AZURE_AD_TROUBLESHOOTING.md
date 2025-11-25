# Azure AD Troubleshooting (Node.js)

Aligned with the NuGet package behavior.

## Checklist
- `authority` and `issuer` use the exact tenant URL (`https://login.microsoftonline.com/<TENANT>/v2.0`).
- `audiences` contains the same API Application ID URI used by the token.
- `requireHttpsMetadata` remains `true` outside local dev.
- Token contains a `kid`; otherwise JWKS lookup will fail.

## Common failures
- **Invalid signature**: token signed by a different tenant or stale key; clear JWKS cache or reduce `jwksCacheTtl`.
- **Token tenant mismatch**: token `tid` differs from authority tenant.
- **Network timeouts**: increase axios timeout or ensure outbound HTTPS connectivity to login.microsoftonline.com.

## Tips
- Use https://jwt.ms to inspect the token; copy `iss`, `aud`, `kid`.
- For multi-tenant apps, configure one issuer entry per tenant.
