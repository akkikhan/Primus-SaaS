# Angular Integration (Node.js Backend)

Use the same flow as the NuGet guide: Angular fetches a token, the Node backend validates it.

## Frontend (Angular)
- Acquire tokens with MSAL (or your IdP SDK).
- Send requests with `Authorization: Bearer <access_token>`.

```typescript
const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
this.http.get('/api/secure', { headers }).subscribe();
```

## Backend (Node)

```typescript
import { primusIdentityMiddleware } from '@primus-saas/identity-validator';

app.use(primusIdentityMiddleware({
  issuers: [{
    name: 'AzureAD',
    type: 'oidc',
    issuer: 'https://login.microsoftonline.com/<TENANT>/v2.0',
    authority: 'https://login.microsoftonline.com/<TENANT>/v2.0',
    audiences: ['api://your-api-id']
  }]
}));
```

## Local JWT + Angular
- Use the same issuer/audience/secret in Angular and Node.
- Generate dev tokens with `jsonwebtoken` and inject into Angular requests.

## Troubleshooting
- 401: check audience/issuer alignment.
- CORS: expose `Authorization` header.
- 429: rate limiting activated; reduce failed attempts or increase window in `rateLimiting`.
