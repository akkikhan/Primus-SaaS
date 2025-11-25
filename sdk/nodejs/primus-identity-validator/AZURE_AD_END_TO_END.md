# Azure AD End-to-End (Node.js)

End-to-end flow to mirror the NuGet experience.

## 1) Configure Azure AD
- Register your API and expose an API scope or App Role.
- Note the Application ID URI (`api://your-api-id`).

## 2) Configure the validator

```typescript
const primusAuth = primusIdentityMiddleware({
  issuers: [{
    name: 'AzureAD',
    type: 'oidc',
    issuer: 'https://login.microsoftonline.com/<TENANT>/v2.0',
    authority: 'https://login.microsoftonline.com/<TENANT>/v2.0',
    audiences: ['api://your-api-id']
  }],
  requireHttpsMetadata: true
});
```

## 3) Protect routes
```typescript
app.get('/api/secure', primusAuth, (req, res) => {
  res.json({ ok: true, user: req.primusUser });
});
```

## 4) Test
- Acquire a token from Azure AD (Postman/MSAL) for the configured audience.
- Send `Authorization: Bearer <token>`.
- Expect 200 with `primusUser` populated.

## 5) Diagnostics
- 401 → check audience/issuer or expired token.
- 429 → reduce failed attempts or adjust `rateLimiting`.
- To test multiple tenants, add multiple issuer entries; the middleware routes by `iss`.
