# Primus SaaS Identity Validator - Node.js SDK

Official Node.js SDK for validating JWT tokens issued by Primus SaaS Portal. This SDK provides Express middleware for seamless authentication integration.

## Features

- 🔐 JWT token validation with configurable options
- ⚡ Express middleware for easy integration
- 🎯 Role-based access control
- 📝 TypeScript support with full type definitions
- ✅ Comprehensive test coverage
- 🔧 Configurable validation parameters

## Installation

```bash
npm install @primus-saas/identity-validator
```

## Quick Start

### Express Application

```typescript
import express from 'express';
import { primusIdentityMiddleware, requireRoles } from '@primus-saas/identity-validator';

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

The SDK accepts the following configuration options:

| Option | Type | Required | Default | Description |
|--------|------|----------|---------|-------------|
| `portalUrl` | string | Yes | - | The base URL of your Primus SaaS Portal |
| `clientId` | string | Yes | - | Your client ID from Primus Portal |
| `clientSecret` | string | Yes | - | Your client secret from Primus Portal |
| `jwtSecret` | string | Yes | - | JWT secret key for token validation |
| `issuer` | string | No | `portalUrl` | Expected token issuer |
| `audience` | string | No | `clientId` | Expected token audience |
| `validateLifetime` | boolean | No | `true` | Whether to validate token expiration |
| `clockSkew` | number | No | `300` | Clock tolerance in seconds (5 minutes) |

### Environment Variables

You can use environment variables for configuration:

```typescript
const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL!,
  clientId: process.env.PRIMUS_CLIENT_ID!,
  clientSecret: process.env.PRIMUS_CLIENT_SECRET!,
  jwtSecret: process.env.PRIMUS_JWT_SECRET!
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
