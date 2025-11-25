# Claims Mapping (Node.js)

The Node.js SDK mirrors the NuGet shape for users and tenants.

## PrimusUser shape

```typescript
interface PrimusUser {
  userId: string;              // sub
  email: string;               // email / upn
  name: string;                // name
  roles: string[];             // role or roles[]
  additionalClaims: Record<string, string>;
}
```

The Express middleware exposes `req.primusUser`.

## Tenant context
- Resolved via `tenantResolver` (if provided).
- Exposed as `req.primusTenantContext` and `req.tenantContext`.

## Mapping guidance
- `sub` → `userId`
- `email` or `upn` → `email`
- `name` → `name`
- `roles` (array) or `role` (string) → `roles`
- Everything else → `additionalClaims`

## Custom role mapping

```typescript
const auth = primusIdentityMiddleware({
  issuers: [...],
  tenantResolver: claims => ({
    tenantId: claims['tid'] as string,
    roles: (claims['role'] as string[] | undefined) ?? []
  })
});
```

## Azure AD tips
- Ensure the Access Token includes `roles` or `groups` claims.
- If using app roles, map `roles` directly; for groups, expand them in the resolver.
