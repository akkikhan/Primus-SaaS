# Primus SaaS Identity Validator - Node.js SDK

**Version:** 1.3.1

Library-only validator for JWT/OIDC tokens from your configured issuers (Azure AD, local, or any JWT provider). No Primus-hosted login or Primus-issued tokens.

> Full client integration guide (Node + .NET + Logging): see `docs-site/docs/modules/client-integration-guide.md`.

## Features

- **Multi-Issuer Support**: Configure multiple identity providers (Azure AD, Local, Custom)
- Express middleware for seamless integration
- Role-based access control
- **Azure AD/OIDC**: JWKS fetching, RS256 validation, tenant verification
- Full TypeScript support with type definitions
- 74 tests passing (100% core logic covered)
- Intelligent JWKS caching (24-hour TTL)

## Installation

```bash
npm install @primus-saas/identity-validator
```

## Quick Start

### Multi-Issuer Configuration (Recommended)

```typescript
import express from 'express';
import { primusIdentityMiddleware } from '@primus-saas/identity-validator';

const app = express();

// Configure multiple trusted issuers
const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0',
      authority: 'https://login.microsoftonline.com/<YOUR_TENANT_ID>/v2.0',
      audiences: ['api://your-app-id']
    },
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: 'https://auth.yourcompany.com',
      secret: process.env.LOCAL_JWT_SECRET,
      audiences: ['api://your-app-id']
    }
  ]
});

// Protected routes
app.get('/api/protected', primusAuth, (req, res) => {
  res.json({ user: req.primusUser });
});

app.listen(3000);
```

## Configuration

### IssuerConfig Options

Each issuer in the issuers array accepts:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| name | string | Yes | Friendly name for this issuer |
| type | 'oidc' \| 'jwt' | Yes | Type of identity provider |
| issuer | string | Yes | Expected iss claim value (used for routing) |
| authority | string | OIDC only | Authority URL for OIDC discovery |
| audiences | string[] | Yes | Valid audience values (aud claim) |
| secret | string | JWT only | Shared secret for HMAC validation |
| jwksUrl | string | Optional | JWKS endpoint (alternative to secret) |

### Global Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| clockSkew | number | 300 | Clock tolerance in seconds |
| validateLifetime | boolean | true | Validate token expiration |
| requireHttpsMetadata | boolean | true | Require HTTPS for OIDC metadata/JWKS (set false for local dev only) |
| jwksCacheTtl | number | 24 | JWKS cache TTL in hours |
| rateLimiting | object | disabled | Throttle repeated failed validations (maxFailuresPerWindow, windowSeconds, maxGlobalFailuresPerWindow) |

## Usage Examples

### Single Azure AD Issuer

```typescript
const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
      authority: 'https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0',
      audiences: ['e2760fbd-f134-42f4-bcda-f44306fc3fe2']
    }
  ],
  clockSkew: 300
});
```

### Single Local JWT Issuer

```typescript
const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: 'http://localhost:4000',
      secret: 'your-secret-key-min-32-chars',
      audiences: ['api://my-app']
    }
  ]
});
```

### Multi-Issuer (Hybrid)

```typescript
const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'AzureAD-Production',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<PROD_TENANT>/v2.0',
      authority: 'https://login.microsoftonline.com/<PROD_TENANT>/v2.0',
      audiences: ['api://prod-app']
    },
    {
      name: 'AzureAD-Development',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<DEV_TENANT>/v2.0',
      authority: 'https://login.microsoftonline.com/<DEV_TENANT>/v2.0',
      audiences: ['api://dev-app']
    },
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: 'http://localhost:4000',
      secret: process.env.LOCAL_SECRET,
      audiences: ['api://dev-app']
    }
  ]
});
```

### Role-Based Access Control

```typescript
import { primusIdentityMiddleware, requireRoles } from '@primus-saas/identity-validator';

// Multiple roles (user needs at least one)
app.get('/api/admin', primusAuth, requireRoles('Admin', 'SuperAdmin'), (req, res) => {
  res.json({ message: 'Admin access granted' });
});

// Single role
app.get('/api/manager', primusAuth, requireRoles('Manager'), (req, res) => {
  res.json({ message: 'Manager access' });
});
```

### Using the Validator Directly

```typescript
import { PrimusIdentityValidator } from '@primus-saas/identity-validator';

const validator = new PrimusIdentityValidator({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<TENANT>/v2.0',
      authority: 'https://login.microsoftonline.com/<TENANT>/v2.0',
      audiences: ['api://my-app']
    }
  ]
});

// Validate token manually (without "Bearer " prefix)
const token = 'eyJ0eXAiOiJKV1QiLCJhbGc...';
const result = await validator.validateToken(token);

if (result.isValid) {
  console.log('Token validated successfully');
  console.log('Claims:', result.claims);
} else {
  console.error('Validation failed:', result.error);
}
```

## How It Works

### Token Routing

1. **Extract iss claim** from JWT (without verifying signature)
2. **Match issuer** against configured issuers array
3. **Route to appropriate validator**:
   - type: 'oidc' -> Fetch JWKS, validate with RS256
   - type: 'jwt' -> Validate with shared secret (HS256)
4. **Verify signature**, issuer, audience, expiration
5. **Return result** with claims or error

### OIDC Validation Flow

For type: 'oidc' issuers:

1. Fetch OpenID configuration from {authority}/.well-known/openid-configuration
2. Retrieve JWKS from jwks_uri
3. Find public key matching token's kid (Key ID)
4. Verify RS256 signature
5. Validate issuer, audience, tenant, expiration
6. Cache JWKS for 24 hours

### JWT Validation Flow

For type: 'jwt' issuers:

1. Verify HMAC signature using shared secret
2. Validate issuer matches configured issuer
3. Validate audience is in audiences array
4. Verify token expiration

## API Reference

### primusIdentityMiddleware(options)

Creates Express middleware that validates tokens and attaches user to req.primusUser.

**Behavior:**
- Extracts token from Authorization: Bearer <token> header
- Routes to correct validator based on iss claim
- Returns 401 if validation fails
- Attaches PrimusUser to req.primusUser on success
- Attaches tenant context to req.primusTenantContext (and req.tenantContext) when a tenantResolver is provided
- Optional rate limiting (429 + Retry-After) for repeated failures via `rateLimiting`

### requireRoles(...roles: string[])

Middleware that enforces role-based access control.

**Returns:**
- 401 if user not authenticated
- 403 if user lacks required role
- Calls next() if user has at least one required role

### PrimusUser Interface

```typescript
interface PrimusUser {
  userId: string;              // Subject (sub claim)
  email: string;               // User email
  name: string;                // Display name
  roles: string[];             // Assigned roles
  additionalClaims: Record<string, any>;  // Other JWT claims
}
```

## Migration from v1.0.0

### Breaking Changes

The configuration structure has changed from single-mode to multi-issuer:

**OLD (v1.0.0):**
```typescript
// Legacy configuration (Removed)
const primusAuth = primusIdentityMiddleware({
  portalUrl: '...',
  clientId: '...',
  clientSecret: '...',
  mode: ValidationMode.AzureAd,
  tenantId: '...'
});
```

**NEW (v1.1.0):**
```typescript
// New configuration
const primusAuth = primusIdentityMiddleware({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<TENANT>/v2.0',
      authority: 'https://login.microsoftonline.com/<TENANT>/v2.0',
      audiences: ['<CLIENT_ID>']
    }
  ]
});
```

### Migration Steps

1. Remove ValidationMode imports (no longer exported)
2. Replace single config object with issuers array
3. For Azure AD: Use type: 'oidc' with authority
4. For Local: Use type: 'jwt' with secret
5. Map clientId -> audiences[0]
6. Map tenantId -> extract from issuer URL

## Generating Tokens for Local JWT Issuer

> **Important:** The secret, issuer, and audiences values used when generating tokens MUST EXACTLY MATCH your validator configuration.

### Quick Example

```javascript
const jwt = require('jsonwebtoken');

function generateLocalJwtToken(userId, email, name) {
  // Critical: Load from same environment variables
  const secret = process.env.JWT_SECRET;
  const issuer = process.env.JWT_ISSUER;
  const audience = process.env.JWT_AUDIENCE;
  
  const payload = {
    sub: userId,
    email: email,
    name: name,
    aud: audience,
    iss: issuer
  };
  
  const options = {
    expiresIn: '1h',
    issuer: issuer,
    audience: audience,
    algorithm: 'HS256'
  };
  
  return jwt.sign(payload, secret, options);
}
```

For complete token generation examples including frontend integration, see [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md)

## Troubleshooting

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| invalid signature | Secret key mismatch | Ensure token generation and validation use the same secret |
| Untrusted issuer | Issuer format incorrect | Use full URL format (e.g., https://localhost:4000) not name |
| jwt audience invalid | Audience mismatch | Use API identifier format (e.g., api://your-app-id) |
| jwt expired | Token past expiration | Generate new token or increase clockSkew |

### "Untrusted issuer: {url}"

**Cause:** Token's iss claim doesn't match any configured issuer.

**Solution:** Add issuer to issuers array with exact iss value:
```typescript
issuers: [
  {
    name: '...',
    type: '...',
    issuer: '<EXACT_ISS_VALUE_FROM_TOKEN>',  // Must match exactly
    // ...
  }
]
```

### "Authority URL required for OIDC issuer"

**Cause:** OIDC issuer missing authority property.

**Solution:** Add authority URL:
```typescript
{
  type: 'oidc',
  authority: 'https://login.microsoftonline.com/<TENANT>/v2.0',  // Required
  issuer: 'https://login.microsoftonline.com/<TENANT>/v2.0',
  // ...
}
```

### "Shared secret required for JWT issuer"

**Cause:** JWT issuer missing secret property.

**Solution:** Provide shared secret:
```typescript
{
  type: 'jwt',
  secret: process.env.JWT_SECRET,  // Required (min 32 chars recommended)
  issuer: 'https://...',
  // ...
}
```

### Token Expired

**Cause:** Token's exp claim is in the past.

**Solution:** Increase clockSkew for clock drift tolerance:
```typescript
{
  issuers: [...],
  clockSkew: 600  // 10 minutes tolerance
}
```

For detailed troubleshooting, see [ERROR_REFERENCE.md](./ERROR_REFERENCE.md)

## Production Deployment

> **Caution:** Never commit secrets to source control! Use environment variables or secret management services.

### Quick Checklist

- [ ] All secrets in environment variables or vault
- [ ] HTTPS enforced in production
- [ ] CORS configured for production domains
- [ ] Rate limiting enabled
- [ ] Logging and monitoring configured

For complete deployment guide, see [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md)

## Development

### Build
```bash
npm run build
```

### Test
```bash
npm test
npm run test:coverage
```

### Lint
```bash
npm run lint
npm run format
```

## Requirements

- Node.js 16.0.0+
- Express 4.18.0+ (for middleware usage)

## Documentation

- [TOKEN_GENERATION_GUIDE.md](./TOKEN_GENERATION_GUIDE.md) - Complete guide to generating JWT tokens
- [ERROR_REFERENCE.md](./ERROR_REFERENCE.md) - Troubleshooting validation errors
- [PRODUCTION_DEPLOYMENT.md](./PRODUCTION_DEPLOYMENT.md) - Production deployment best practices

## License

MIT

## Support

- Documentation: https://docs.primus-saas.com
- Issues: https://github.com/akkikhan/Primus-SaaS/issues
- Email: support@primus-saas.com
