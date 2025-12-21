---
id: identity-validator
title: Identity Validator - Overview
sidebar_position: 0
description: Multi-issuer JWT/OIDC validation for .NET and Node.js (Azure AD + any OIDC authority + Local/JWKS JWT).
---

# Identity Validator - Overview

Multi-issuer JWT/OIDC validation for your APIs. Works fully in-process—no Primus-hosted runtime. Supports Azure AD/Microsoft Entra, any OIDC authority (Auth0, Google, Cognito, etc.), and Local/JWKS JWT issuers.

## Packages

- .NET: `PrimusSaaS.Identity.Validator` (current: 1.5.0)
- Node.js: `@primus-saas/identity-validator` (current: 1.3.3)

## Key Capabilities

- Multiple issuers with per-issuer audiences
- OIDC discovery + JWKS validation (RS/ES algos); Azure path retains tenant verification
- Local/JWKS JWT validation with shared secret or remote JWKS
- Multi-audience validation (arrays)
- Tenant resolution hook and rate limiting (per-SDK)
- Express middleware (Node) and ASP.NET Core middleware/extensions (.NET)

## Quick Install

```bash
# .NET
dotnet add package PrimusSaaS.Identity.Validator --version 1.5.0

# Node.js
npm install @primus-saas/identity-validator
```

## Node.js – Generic OIDC Example

```ts
import express from "express";
import { primusIdentityMiddleware } from "@primus-saas/identity-validator";

const app = express();

const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: "Auth0",
      type: "oidc",
      issuer: "https://your-tenant.us.auth0.com/",
      authority: "https://your-tenant.us.auth0.com/",
      audiences: ["https://api.your-app.com", "api://your-app-id"],
    },
  ],
});

app.get("/secure", primusAuth, (req, res) =>
  res.json({ user: req.primusUser }),
);
app.listen(3000);
```

## Azure AD (Node.js and .NET)

- Azure path remains intact; tenant extracted from `authority` and issuer validated against Azure v1/v2/STS endpoints.
- For Node.js, use `authority` like `https://login.microsoftonline.com/<tenant>/v2.0` and `audiences: ['api://...']`.

## Local / JWKS JWT

- Provide `secret` for HMAC or `jwksUrl` for RSA/EC keys.
- Audience arrays are supported.

## See Also

- `identity-quick-start.md` (overview)
- `identity-azure-ad.md`, `identity-auth0.md`, `identity-local-jwt.md`, `identity-multi-issuer.md`
