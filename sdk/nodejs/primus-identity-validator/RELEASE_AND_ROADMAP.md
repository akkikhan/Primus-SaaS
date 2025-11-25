# Release & Roadmap (Node.js)

This Node.js SDK tracks the NuGet Identity Validator feature set.

## Current (1.3.2)
- Multi-issuer (OIDC + JWT) validation.
- JWKS caching with configurable TTL.
- Tenant resolver with request attachment (`req.primusTenantContext`).
- Rate limiting for failed authentications (disabled by default).
- HTTPS metadata enforcement toggle (`requireHttpsMetadata`).
- Full documentation parity with NuGet package.

## Near-term
- Additional diagnostics hooks (structured security events).
- Pluggable metrics sink for validation outcomes.

## Versioning
- Version numbers stay aligned to NuGet releases where possible.
- Breaking changes will increment the minor/major version; config defaults remain backward compatible.
