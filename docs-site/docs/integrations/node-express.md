---
id: node-express
title: Node.js (Express)
---

Build a protected API with the Node.js validator and the same multi-issuer config used in `test-apps/acme-dashboard/server.js`.

## Install

```bash
npm install primus-identity-validator express
```

## Wire up Express

```javascript
// server.js
const express = require('express');
const { primusIdentityMiddleware, requireRoles } = require('primus-identity-validator');

const app = express();

const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
      authority: 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
      audiences: ['acc675f1-e32f-40b9-a0c6-716066cc6890']
    },
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: 'http://localhost:4000',
      secret: process.env.LOCAL_JWT_SECRET || 'local-dev-secret-123',
      audiences: ['acc675f1-e32f-40b9-a0c6-716066cc6890']
    }
  ],
  clockSkew: 300
});

// Public endpoint
app.get('/api/public', (_req, res) => {
  res.json({ message: 'No auth required' });
});

// Protected endpoint
app.get('/api/protected', primusAuth, (req, res) => {
  res.json({
    message: 'Authenticated',
    user: req.primusUser
  });
});

// Role-gated endpoint
app.get('/api/admin', primusAuth, requireRoles('Admin'), (_req, res) => {
  res.json({ message: 'Admin only' });
});

app.listen(3000, () => {
  console.log('API listening on http://localhost:3000');
});
```

## Test locally

1. Start your LocalAuth/JWT issuer on `http://localhost:4000` and create a token with `iss=http://localhost:4000`, `aud=acc675f1-e32f-40b9-a0c6-716066cc6890`.
2. Hit `GET /api/protected` with `Authorization: Bearer <token>`.
3. Repeat with an Azure AD token whose `iss` matches the configured tenant; the middleware will auto-route to OIDC validation.

## Harden for production

- Keep `clockSkew` small (`<=300s`) and enforce `https` for issuers.
- Store secrets in env vars or a secret manager; never commit them.
- Use multiple audiences when serving mobile/web clients that share the same API.
