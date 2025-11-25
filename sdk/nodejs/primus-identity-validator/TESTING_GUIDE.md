# Testing Guide (Node.js)

This guide mirrors the NuGet testing flow using the Node.js SDK.

## Local prerequisites
- Node.js 16+
- `npm install`

## Run automated tests

```bash
cd sdk/nodejs/primus-identity-validator
npm test
npm run test:coverage
```

## Smoke test with a local JWT issuer

```javascript
const jwt = require('jsonwebtoken');
const token = jwt.sign(
  { sub: 'user-1', email: 'demo@example.com', iss: 'https://auth.local', aud: 'api://sample' },
  process.env.JWT_SECRET,
  { expiresIn: '1h' }
);
```

Configure middleware:

```typescript
primusIdentityMiddleware({
  issuers: [{
    name: 'LocalAuth',
    type: 'jwt',
    issuer: 'https://auth.local',
    secret: process.env.JWT_SECRET!,
    audiences: ['api://sample']
  }]
});
```

Hit a protected route with `Authorization: Bearer <token>` and expect 200.

## Azure AD smoke test
- Set `authority` and `issuer` to the exact tenant URL (`https://login.microsoftonline.com/<TENANT>/v2.0`).
- Use a token from `https://jwt.ms` or Postman with the same audience.
- Expect 401 for bad audience, 429 if rate limit exceeded.

## What to validate
- 401 on missing/invalid token.
- 429 after repeated failures when `rateLimiting.enabled = true`.
- `req.primusUser` and `req.primusTenantContext` populated for valid tokens.
- JWKS caching working across repeated validations (no repeated network fetches).
