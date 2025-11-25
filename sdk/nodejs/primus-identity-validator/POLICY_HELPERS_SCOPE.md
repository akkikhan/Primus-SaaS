# Policy Helpers & Scope (Node.js)

The Node.js SDK mirrors the NuGet role/scope checks through lightweight helpers.

## Role enforcement

```typescript
import { primusIdentityMiddleware, requireRoles } from '@primus-saas/identity-validator';

app.get('/admin', primusIdentityMiddleware(opts), requireRoles('Admin'), handler);
app.get('/support', primusIdentityMiddleware(opts), requireRoles('Support', 'Admin'), handler);
```

- 401 if no token.
- 403 if token is valid but missing required roles.

## Custom policies
- Implement your own middleware using `req.primusUser` and `req.primusTenantContext`.
- Combine with logging to record policy failures.

## Scopes vs roles
- For Azure AD scopes, expose them as roles/claims in your token, then map via `tenantResolver` if needed.
