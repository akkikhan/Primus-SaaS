# Tenant Resolver Guide (Node.js)

The Node.js SDK matches the NuGet package by allowing you to resolve tenant context from JWT claims and attach it to the request pipeline.

## Why use a tenant resolver?
- Normalize claims from different issuers into a single `{ tenantId, roles, metadata }` shape.
- Enforce tenant isolation and pass context to logging middleware (`req.primusTenantContext`).

## Quick Start

```typescript
import { primusIdentityMiddleware } from '@primus-saas/identity-validator';

const auth = primusIdentityMiddleware({
  issuers: [
    { name: 'AzureAD', type: 'oidc', issuer: 'https://login.microsoftonline.com/<tenant>/v2.0', authority: 'https://login.microsoftonline.com/<tenant>/v2.0', audiences: ['api://my-api'] },
    { name: 'LocalAuth', type: 'jwt', issuer: 'https://auth.local', secret: process.env.JWT_SECRET!, audiences: ['api://my-api'] }
  ],
  tenantResolver: (claims) => ({
    tenantId: (claims['tid'] as string) || (claims['tenant'] as string) || 'default',
    roles: (claims['roles'] as string[]) || [],
    metadata: { issuer: claims['iss'] }
  })
});
```

## Behavior
- Resolver is called **after** signature/issuer/audience validation.
- If the resolver throws, the request is rejected (401) to match NuGet behavior.
- Resolved context is attached to `req.primusTenantContext` and `req.tenantContext`.
- Logging middleware reads `req.primusTenantContext` automatically.

## Tips
- Prefer stable claim names (`tid`, `tenantId`) over display names.
- Keep resolver side-effect free; cache external lookups outside the function.
- For multi-tenant Azure AD, map `tid` → friendly tenant metadata from your store.
