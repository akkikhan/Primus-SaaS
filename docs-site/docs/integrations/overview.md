---
id: overview
title: Integration Overview
---

Primus authenticates requests by routing JWT/OIDC tokens to the correct issuer based on the `iss` claim, validating signatures, audiences, lifetimes, and then attaching a normalized `PrimusUser` to the request context.

## Steps in every stack

1. **Install the validator** (`primus-identity-validator` for Node.js or `PrimusSaaS.Identity.Validator` for .NET).
2. **Configure issuers** with the exact `issuer` (iss) value plus either an OIDC `authority` or a JWT `secret`/`jwksUrl`.
3. **Add middleware** to protect endpoints and optional role gates.
4. **Test with both issuers**: Azure AD token + LocalAuth/JWT to confirm routing works.
5. **Tune security**: enable HTTPS metadata in non-local environments, keep `clockSkew` modest (default 300s).

## Sample multi-issuer config

```json
{
  "issuers": [
    {
      "name": "AzureAD",
      "type": "oidc",
      "issuer": "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0",
      "authority": "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0",
      "audiences": ["acc675f1-e32f-40b9-a0c6-716066cc6890"]
    },
    {
      "name": "LocalAuth",
      "type": "jwt",
      "issuer": "http://localhost:4000",
      "secret": "local-dev-secret-123",
      "audiences": ["acc675f1-e32f-40b9-a0c6-716066cc6890"]
    }
  ],
  "clockSkew": 300
}
```

Re-use this structure in both stacks; only the wiring into the framework changes.
