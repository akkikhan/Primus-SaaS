# Primus SaaS Identity Validator - Node.js SDK

Official Node.js SDK for validating JWT tokens issued by Primus SaaS Portal. This SDK provides Express middleware for seamless authentication integration.

## Features

- 🔐 **Dual Validation Modes**: Local JWT and Azure AD token validation
- ⚡ Express middleware for easy integration
- 🎯 Role-based access control
- 🔑 **Azure AD Support**: JWKS fetching, RS256 signature verification, tenant validation
- 📝 TypeScript support with full type definitions
- ✅ Comprehensive test coverage (83 tests, 99.18% coverage)
- 🔧 Configurable validation parameters
- ⚙️ **Hybrid Mode**: Automatic fallback between Azure AD and Local validation
- 🚀 Performance optimized with intelligent caching (24-hour TTL)

## Installation

```bash
npm install primus-identity-validator
```

## Quick Start

### Express Application

```typescript
import express from 'express';
import { primusIdentityMiddleware, requireRoles } from 'primus-identity-validator';

const app = express();

// Configure Primus identity validation
const primusAuth = primusIdentityMiddleware({
  portalUrl: 'https://portal.primus-saas.com',
  clientId: 'your-client-id',
  clientSecret: 'your-client-secret',
  jwtSecret: 'your-jwt-secret'
});

// Apply middleware to protected routes
app.use('/api/protected', primusAuth);

// Protected route - user information available in req.primusUser
app.get('/api/protected/profile', (req, res) => {
  res.json({
    user: req.primusUser
  });
});

// Admin-only route
app.get('/api/admin', primusAuth, requireRoles('Admin'), (req, res) => {
  res.json({ message: 'Admin access granted' });
});

app.listen(3000);
```

### Configuration

The SDK supports three validation modes:

1. **Local Mode** (default): Validates JWT tokens using symmetric HMAC signature
2. **Azure AD Mode**: Validates Azure AD tokens using asymmetric RSA signatures with JWKS
3. **Hybrid Mode**: Tries Azure AD first, falls back to Local validation

The SDK accepts the following configuration options:

| Option | Type | Required | Default | Description |
|--------|------|----------|---------|-------------|
| `portalUrl` | string | Yes | - | The base URL of your Primus SaaS Portal |
| `clientId` | string | Yes | - | Your client ID from Primus Portal |
| `clientSecret` | string | Yes | - | Your client secret from Primus Portal |
| `mode` | ValidationMode | No | `Local` | Validation mode: `Local`, `AzureAd`, or `Hybrid` |
| `tenantId` | string | Conditional* | - | Azure AD tenant ID (required for AzureAd/Hybrid modes) |
| `jwtSecret` | string | Conditional* | - | JWT secret key (required for Local/Hybrid modes) |
| `jwksCacheTtl` | number | No | `24` | JWKS cache TTL in hours |
| `issuer` | string | No | `portalUrl` | Expected token issuer (Local mode only) |
| `audience` | string | No | `clientId` | Expected token audience |
| `validateLifetime` | boolean | No | `true` | Whether to validate token expiration |
| `clockSkew` | number | No | `300` | Clock tolerance in seconds (5 minutes) |

\* **Conditional Requirements**:
- `jwtSecret`: Required for **Local** and **Hybrid** modes
- `tenantId`: Required for **AzureAd** and **Hybrid** modes

### Azure AD Mode Configuration

For production applications using Azure AD authentication:

```typescript
import { primusIdentityMiddleware, ValidationMode } from 'primus-identity-validator';

const primusAuth = primusIdentityMiddleware({
  portalUrl: 'https://portal.primus-saas.com',
  clientId: 'your-azure-ad-client-id',        // Azure AD Application (Client) ID
  clientSecret: 'your-client-secret',
  mode: ValidationMode.AzureAd,
  tenantId: 'your-azure-ad-tenant-id',        // Azure AD Tenant ID
  jwksCacheTtl: 24                            // Cache JWKS keys for 24 hours
});

app.use('/api', primusAuth);
```

**How Azure AD Validation Works:**
1. SDK extracts the `kid` (Key ID) from the token header
2. Fetches OpenID Connect configuration from Azure AD (`/.well-known/openid-configuration`)
3. Retrieves JWKS (JSON Web Key Set) containing public keys
4. Validates token signature using RS256 algorithm
5. Verifies issuer, audience, expiration, and tenant ID
6. Caches JWKS keys for 24 hours (configurable) to minimize latency

**Supported Azure AD Issuers:**
- `https://login.microsoftonline.com/{tenant}/v2.0` (v2 endpoint)
- `https://login.microsoftonline.com/{tenant}/` (v1 endpoint)
- `https://sts.windows.net/{tenant}/` (legacy)

### Hybrid Mode Configuration

For applications that accept both Primus Portal tokens and Azure AD tokens:

```typescript
const primusAuth = primusIdentityMiddleware({
  portalUrl: 'https://portal.primus-saas.com',
  clientId: 'your-client-id',
  clientSecret: 'your-client-secret',
  mode: ValidationMode.Hybrid,
  tenantId: 'your-azure-ad-tenant-id',        // Required for Azure AD validation
  jwtSecret: 'your-jwt-secret',               // Required for Local validation
  jwksCacheTtl: 24
});
```

**Hybrid Mode Behavior:**
1. Attempts Azure AD validation first
2. If Azure AD validation fails, falls back to Local validation
3. Returns the first successful validation result
4. Useful for migration scenarios or multi-tenant applications

### Environment Variables

You can use environment variables for configuration:

```typescript
// Local Mode
const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL!,
  clientId: process.env.PRIMUS_CLIENT_ID!,
  clientSecret: process.env.PRIMUS_CLIENT_SECRET!,
  jwtSecret: process.env.PRIMUS_JWT_SECRET!
});

// Azure AD Mode
const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL!,
  clientId: process.env.AZURE_AD_CLIENT_ID!,
  clientSecret: process.env.PRIMUS_CLIENT_SECRET!,
  mode: ValidationMode.AzureAd,
  tenantId: process.env.AZURE_AD_TENANT_ID!,
  jwksCacheTtl: 24
});
```

## API Reference

### `primusIdentityMiddleware(options)`

Creates Express middleware that validates JWT tokens and attaches user information to `req.primusUser`.

**Parameters:**
- `options` (PrimusIdentityOptions): Configuration options

**Returns:**
- Express middleware function

**Behavior:**
- Extracts JWT token from `Authorization: Bearer <token>` header
- Validates token signature, expiration, issuer, and audience
- Attaches decoded user to `req.primusUser`
- Returns 401 if authentication fails

### `requireRoles(...roles)`

Creates Express middleware that checks if the authenticated user has at least one of the specified roles.

**Parameters:**
- `...roles` (string[]): Required role names

**Returns:**
- Express middleware function

**Behavior:**
- Returns 401 if user is not authenticated
- Returns 403 if user lacks required roles
- Calls `next()` if user has at least one required role

### `PrimusUser` Interface

The user object attached to `req.primusUser`:

```typescript
interface PrimusUser {
  userId: string;           // Unique user ID
  email: string;            // User's email
  name: string;             // User's full name
  roles: string[];          // Assigned roles
  additionalClaims: Record<string, string>; // Extra JWT claims
}
```

## Usage Examples

### Basic Protected Route

```typescript
app.get('/api/data', primusAuth, (req, res) => {
  // Access authenticated user
  const user = req.primusUser;
  
  res.json({
    message: `Hello ${user.name}`,
    userId: user.userId
  });
});
```

### Role-Based Access

```typescript
// Multiple roles - user needs at least one
app.get('/api/admin', primusAuth, requireRoles('Admin', 'SuperAdmin'), (req, res) => {
  res.json({ message: 'Admin access' });
});

// Single role
app.get('/api/manager', primusAuth, requireRoles('Manager'), (req, res) => {
  res.json({ message: 'Manager access' });
});
```

### Custom Error Handling

```typescript
app.use((err, req, res, next) => {
  if (err.name === 'UnauthorizedError') {
    res.status(401).json({ error: 'Invalid token' });
  } else {
    next(err);
  }
});
```

### Client Usage

```typescript
import axios from 'axios';

// Obtain token from Primus Portal login
const token = 'your-jwt-token';

// Make authenticated request
const response = await axios.get('https://api.example.com/protected', {
  headers: {
    Authorization: `Bearer ${token}`
  }
});
```

## Azure AD Integration Examples

### Complete Azure AD Application Example

```typescript
import express from 'express';
import { primusIdentityMiddleware, requireRoles, ValidationMode } from '@primus-saas/identity-validator';

const app = express();

// Configure Azure AD authentication
const azureAuth = primusIdentityMiddleware({
  portalUrl: 'https://portal.primus-saas.com',
  clientId: process.env.AZURE_AD_CLIENT_ID!,      // e.g., 'e2760fbd-f134-42f4-bcda-f44306fc3fe2'
  clientSecret: process.env.PRIMUS_CLIENT_SECRET!,
  mode: ValidationMode.AzureAd,
  tenantId: process.env.AZURE_AD_TENANT_ID!,      // e.g., 'cbd15a9b-cd52-4ccc-916a-00e2edb13043'
  jwksCacheTtl: 24,                               // Cache keys for 24 hours
  clockSkew: 300                                   // 5 minutes clock tolerance
});

// Public endpoint (no authentication)
app.get('/api/public', (req, res) => {
  res.json({ message: 'Public data' });
});

// Protected endpoint (requires valid Azure AD token)
app.get('/api/user/profile', azureAuth, (req, res) => {
  const user = req.primusUser;
  res.json({
    userId: user.userId,
    email: user.email,
    name: user.name,
    roles: user.roles
  });
});

// Admin-only endpoint
app.get('/api/admin/settings', azureAuth, requireRoles('Admin'), (req, res) => {
  res.json({ message: 'Admin settings' });
});

app.listen(3000, () => {
  console.log('Server running on http://localhost:3000');
});
```

### Azure AD Token Acquisition (Client-Side)

To obtain an Azure AD token for testing:

```bash
# Using Azure CLI
az account get-access-token --resource "api://your-client-id" --query accessToken -o tsv
```

Or using MSAL (Microsoft Authentication Library):

```typescript
import { PublicClientApplication } from '@azure/msal-node';

const msalConfig = {
  auth: {
    clientId: 'your-azure-ad-client-id',
    authority: 'https://login.microsoftonline.com/your-tenant-id'
  }
};

const pca = new PublicClientApplication(msalConfig);

// Device code flow for CLI applications
const deviceCodeRequest = {
  deviceCodeCallback: (response) => {
    console.log(response.message);
  },
  scopes: ['api://your-client-id/.default']
};

const response = await pca.acquireTokenByDeviceCode(deviceCodeRequest);
const accessToken = response.accessToken;
```

### Debugging Azure AD Validation

```typescript
import { PrimusIdentityValidator, ValidationMode } from '@primus-saas/identity-validator';

const validator = new PrimusIdentityValidator({
  portalUrl: 'https://portal.primus-saas.com',
  clientId: 'your-client-id',
  clientSecret: 'your-client-secret',
  mode: ValidationMode.AzureAd,
  tenantId: 'your-tenant-id'
});

// Validate token manually
const token = 'eyJ0eXAiOiJKV1QiLCJhbGc...';
const result = await validator.validateToken(token);

if (result.isValid) {
  console.log('Token is valid');
  console.log('Claims:', result.claims);
} else {
  console.error('Token validation failed:', result.error);
}
```

### Common Azure AD Configuration Issues

**Issue**: "No matching key found for kid: xxx"
- **Cause**: JWKS cache may be stale or kid doesn't exist
- **Solution**: Wait for cache to expire (24 hours) or restart application

**Issue**: "Token tenant ID does not match expected tenant ID"
- **Cause**: Token was issued for a different Azure AD tenant
- **Solution**: Verify `tenantId` configuration matches the token's `tid` claim

**Issue**: "Token audience does not match"
- **Cause**: Token's `aud` claim doesn't match `clientId`
- **Solution**: Ensure token is requested with correct scope: `api://your-client-id/.default`

**Issue**: "jwt expired"
- **Cause**: Token has expired
- **Solution**: Request a new token or increase `clockSkew` for clock drift tolerance

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

- Node.js 16.0.0 or higher
- Express 4.18.0 or higher (for Express middleware)

## License

MIT

## Support

- Documentation: [https://docs.primus-saas.com](https://docs.primus-saas.com)
- Issues: [https://github.com/akkikhan/Primus-SaaS/issues](https://github.com/akkikhan/Primus-SaaS/issues)
- Email: support@primus-saas.com
