# Primus SaaS Node.js Express Example

This is an example Express.js application demonstrating how to use the **@primus-saas/identity-validator** package to secure your API with JWT authentication from Primus Portal.

## Features Demonstrated

- ✅ JWT Bearer authentication middleware
- ✅ Protected routes requiring authentication
- ✅ Role-based authorization (RBAC)
- ✅ User information extraction from JWT tokens
- ✅ Public and private endpoint examples
- ✅ TypeScript support with type safety
- ✅ Environment variable configuration
- ✅ Request logging with Morgan

## Prerequisites

- Node.js 16 or later
- npm or yarn
- Primus Portal account with application credentials
- JWT secret key from your Primus Portal application

## Getting Started

### 1. Install Dependencies

```bash
cd examples/nodejs-express
npm install
```

This will install:
- `express` - Web framework
- `dotenv` - Environment variable management
- `morgan` - HTTP request logger
- TypeScript and development tools

### 2. Configure Environment Variables

Create a `.env` file by copying the example:

```bash
cp .env.example .env
```

Update the `.env` file with your Primus Portal credentials:

```env
PRIMUS_PORTAL_URL=https://portal.primus-saas.com
PRIMUS_CLIENT_ID=your-actual-client-id
PRIMUS_CLIENT_SECRET=your-actual-client-secret
PRIMUS_JWT_SECRET=your-actual-jwt-secret
PORT=3000
NODE_ENV=development
```

### 3. Run the Application

#### Development mode (with ts-node):
```bash
npm run dev
```

#### Production mode:
```bash
npm run build
npm start
```

The server will start on `http://localhost:3000` (or the PORT specified in .env).

## API Endpoints

### Public Endpoint

**GET** `/api/public` - No authentication required

```bash
curl http://localhost:3000/api/public
```

Response:
```json
{
  "message": "This is a public endpoint accessible without authentication",
  "timestamp": "2025-11-15T09:30:00.000Z"
}
```

### Protected Endpoint

**GET** `/api/protected` - Requires authentication

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" http://localhost:3000/api/protected
```

Response:
```json
{
  "message": "This endpoint requires authentication",
  "user": {
    "userId": "123",
    "email": "user@example.com",
    "name": "John Doe",
    "roles": ["User"]
  }
}
```

### Admin Endpoint

**GET** `/api/admin` - Requires Admin role

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" http://localhost:3000/api/admin
```

### Management Endpoint

**GET** `/api/management` - Requires Manager or Admin role

Demonstrates requiring one of multiple roles.

### Weather Endpoints

**GET** `/api/weather` - Requires authentication

Returns 5-day weather forecast.

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" http://localhost:3000/api/weather
```

**GET** `/api/weather/extended` - Requires Admin or Manager role

Returns 14-day weather forecast with role verification.

## Code Walkthrough

### Configuration (src/index.ts)

```typescript
import { primusIdentityMiddleware, requireRoles } from '@primus-saas/identity-validator';

// Configure authentication middleware
const primusAuth = primusIdentityMiddleware({
  portalUrl: process.env.PRIMUS_PORTAL_URL || 'https://portal.primus-saas.com',
  clientId: process.env.PRIMUS_CLIENT_ID || '',
  clientSecret: process.env.PRIMUS_CLIENT_SECRET || '',
  jwtSecret: process.env.PRIMUS_JWT_SECRET || '',
});
```


### Protected Routes

```typescript
// Simple authentication - requires valid JWT
app.get('/api/protected', primusAuth, (req, res) => {
  // Access user information
  const user = req.user;
  res.json({ message: 'Protected data', user });
});

// Role-based authentication - requires specific role
app.get('/api/admin', primusAuth, requireRoles('Admin'), (req, res) => {
  res.json({ message: 'Admin data', user: req.user });
});

// Multiple role options - requires any of the specified roles
app.get('/api/management', primusAuth, requireRoles('Manager', 'Admin'), (req, res) => {
  res.json({ message: 'Management data', user: req.user });
});
```

### TypeScript Type Safety

```typescript
import type { PrimusUser } from '@primus-saas/identity-validator';

// Extend Express Request to include user
declare global {
  namespace Express {
    interface Request {
      user?: PrimusUser;
    }
  }
}

// Now TypeScript knows about req.user
app.get('/api/protected', primusAuth, (req, res) => {
  console.log(req.user?.userId);  // ✅ Type-safe access
  console.log(req.user?.email);   // ✅ Type-safe access
  console.log(req.user?.roles);   // ✅ Type-safe access
});
```

## Testing with JWT Tokens

### Option 1: Using curl

```bash
# Set your token
export TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Test protected endpoint
curl -H "Authorization: Bearer $TOKEN" http://localhost:3000/api/protected

# Test admin endpoint (requires Admin role in token)
curl -H "Authorization: Bearer $TOKEN" http://localhost:3000/api/admin
```


### Option 2: Using Postman or Insomnia

1. Create a new GET request
2. Set URL to `http://localhost:3000/api/protected`
3. Add header: `Authorization` with value `Bearer YOUR_TOKEN`
4. Send request

### Option 3: Creating Test Tokens

For development, create test JWT tokens using your JwtSecret at [jwt.io](https://jwt.io):

**Token Payload:**
```json
{
  "sub": "user-id-123",
  "email": "test@example.com",
  "name": "Test User",
  "role": ["Admin", "User"],
  "iss": "https://portal.primus-saas.com",
  "aud": "your-client-id",
  "exp": 1700000000
}
```

**Important:** Use your actual `PRIMUS_JWT_SECRET` as the signing key.

## Project Structure

```
nodejs-express/
├── src/
│   └── index.ts                 # Main application file
├── dist/                        # Compiled JavaScript output
├── .env.example                 # Environment variable template
├── .gitignore                   # Git ignore patterns
├── package.json                 # Dependencies and scripts
├── tsconfig.json                # TypeScript configuration
└── README.md                    # This file
```

## Available Scripts

- `npm run build` - Compile TypeScript to JavaScript
- `npm start` - Run compiled JavaScript application
- `npm run dev` - Run with ts-node for development
- `npm run watch` - Watch mode for TypeScript compilation
- `npm run clean` - Remove dist directory

## Key Concepts

### 1. Middleware Configuration

```typescript
const primusAuth = primusIdentityMiddleware(options);
```

Creates middleware that validates JWT tokens and attaches user to `req.user`.


### 2. Applying Middleware

```typescript
// Apply to specific route
app.get('/api/protected', primusAuth, handler);

// Apply to multiple routes
app.use('/api', primusAuth); // All /api/* routes require auth
```

### 3. Role-Based Access Control

```typescript
// Require single role
app.get('/api/admin', primusAuth, requireRoles('Admin'), handler);

// Require any of multiple roles
app.get('/api/management', primusAuth, requireRoles('Manager', 'Admin'), handler);
```

### 4. User Information Access

```typescript
app.get('/api/user-info', primusAuth, (req, res) => {
  const { userId, email, name, roles, additionalClaims } = req.user!;
  res.json({ userId, email, name, roles });
});
```

## Error Responses

### 401 Unauthorized

Returned when:
- No Authorization header present
- Invalid JWT token
- Expired token
- Invalid signature

```json
{
  "error": "Unauthorized",
  "message": "Invalid or missing token"
}
```

### 403 Forbidden

Returned when:
- User is authenticated but lacks required role

```json
{
  "error": "Forbidden",
  "message": "Insufficient permissions"
}
```

## Troubleshooting

### Token Validation Fails

- Verify `PRIMUS_JWT_SECRET` matches the key used to sign tokens
- Check token hasn't expired (exp claim)
- Ensure token format is correct: `Bearer <token>`
- Verify issuer and audience match configuration


### TypeScript Compilation Errors

- Run `npm install` to ensure all dependencies are installed
- Check `tsconfig.json` configuration
- Verify TypeScript version compatibility

### Port Already in Use

- Change PORT in `.env` file
- Or use: `PORT=3001 npm run dev`

## Next Steps

- Add database integration for user data
- Implement refresh token handling
- Add rate limiting middleware
- Integrate logging service (Winston, Bunyan)
- Add API documentation (Swagger/OpenAPI)
- Implement CORS configuration
- Add unit and integration tests
- Deploy to production (Heroku, AWS, Azure)

## Learn More

- [@primus-saas/identity-validator Documentation](../../sdk/nodejs/primus-identity-validator/README.md)
- [Express.js Documentation](https://expressjs.com/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [TypeScript Handbook](https://www.typescriptlang.org/docs/)

## License

MIT
