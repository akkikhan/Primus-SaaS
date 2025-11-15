# SDK Integration Testing Guide

This directory contains test applications for verifying the Primus Identity Validator SDKs before publication.

## Test Applications

### 1. .NET Test App (`dotnet-test-app/`)
- **Framework**: ASP.NET Core Web API (.NET 7.0)
- **Port**: https://localhost:7xxx (auto-assigned)
- **SDK**: Local reference to `../../sdk/dotnet/PrimusSaaS.Identity.Validator/`

**Endpoints**:
- `GET /api/health` - Health check (no auth)
- `GET /api/public` - Public endpoint (no auth)
- `GET /api/protected` - Protected endpoint (auth required)
- `GET /api/admin` - Admin only (Admin role required)
- `GET /api/manager` - Manager or Admin (Manager/Admin role required)

### 2. Node.js Test App (`nodejs-test-app/`)
- **Framework**: Express.js with TypeScript
- **Port**: http://localhost:3001
- **SDK**: Local reference to `../../sdk/nodejs/primus-identity-validator/dist/`

**Endpoints**:
- `GET /api/health` - Health check (no auth)
- `GET /api/public` - Public endpoint (no auth)
- `GET /api/protected` - Protected endpoint (auth required)
- `GET /api/admin` - Admin only (Admin role required)
- `GET /api/manager` - Manager or Admin (Manager/Admin role required)

## Running the Test Apps

### .NET Test App

```bash
cd test-apps/dotnet-test-app/PrimusTest.Api
dotnet run
```

Access at: https://localhost:7xxx (check console output for actual port)

### Node.js Test App

```bash
cd test-apps/nodejs-test-app
npm run dev
```

Access at: http://localhost:3001

## Testing with JWT Tokens

### Option 1: Generate Test Tokens with Portal Backend

1. Start the Portal backend:
   ```bash
   cd portal/backend
   dotnet run
   ```

2. Login to get JWT token:
   ```bash
   curl -X POST https://localhost:7001/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "admin@primussaas.com", "password": "Admin123!"}'
   ```

3. Use the returned token in Authorization header.

### Option 2: Generate Test Tokens with jwt.io

Visit [jwt.io](https://jwt.io) and create a token with:

**Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload:**
```json
{
  "sub": "test-user-123",
  "email": "test@example.com",
  "name": "Test User",
  "role": ["Admin"],
  "aud": "test-client-123",
  "iss": "https://localhost:7001",
  "exp": 1799999999,
  "iat": 1700000000
}
```

**Secret:** `test-jwt-secret-key-with-at-least-32-characters-long`

### Option 3: PowerShell Script (Included)

See `generate-test-token.ps1` for automated token generation.

## Test Scenarios

### Scenario 1: Public Endpoint (No Auth)

**.NET:**
```bash
curl https://localhost:7xxx/api/public
```

**Node.js:**
```bash
curl http://localhost:3001/api/public
```

**Expected**: 200 OK with public message

---

### Scenario 2: Protected Endpoint (Valid Token)

**.NET:**
```bash
curl https://localhost:7xxx/api/protected \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Node.js:**
```bash
curl http://localhost:3001/api/protected \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

**Expected**: 200 OK with user information (userId, email, name, roles)

---

### Scenario 3: Protected Endpoint (No Token)

**.NET:**
```bash
curl https://localhost:7xxx/api/protected
```

**Node.js:**
```bash
curl http://localhost:3001/api/protected
```

**Expected**: 401 Unauthorized

---

### Scenario 4: Admin Endpoint (Admin Role)

**.NET:**
```bash
curl https://localhost:7xxx/api/admin \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**Node.js:**
```bash
curl http://localhost:3001/api/admin \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**Expected**: 200 OK with admin access message

---

### Scenario 5: Admin Endpoint (Non-Admin Role)

Create a token with `"role": ["User"]` and try accessing admin endpoint.

**Expected**: 403 Forbidden

---

### Scenario 6: Manager Endpoint (Manager or Admin)

Test with:
- Token with `"role": ["Manager"]` → Should succeed
- Token with `"role": ["Admin"]` → Should succeed
- Token with `"role": ["User"]` → Should fail (403)

---

## Integration Test Checklist

Before publishing the SDKs, verify:

### .NET SDK
- [x] SDK builds successfully
- [ ] SDK integrates into fresh project
- [ ] Public endpoints work (no auth)
- [ ] Protected endpoints require authentication
- [ ] Valid JWT tokens are accepted
- [ ] Invalid tokens are rejected (401)
- [ ] User information is extracted correctly
- [ ] Role-based authorization works
- [ ] Admin role is enforced
- [ ] Multiple roles work (Manager, Admin)
- [ ] Missing Authorization header returns 401
- [ ] Expired tokens are rejected

### Node.js SDK
- [x] SDK builds successfully
- [ ] SDK integrates into fresh project
- [ ] Public endpoints work (no auth)
- [ ] Protected endpoints require authentication
- [ ] Valid JWT tokens are accepted
- [ ] Invalid tokens are rejected (401)
- [ ] User information is extracted correctly
- [ ] Role-based authorization works
- [ ] Admin role is enforced
- [ ] Multiple roles work (Manager, Admin)
- [ ] Missing Authorization header returns 401
- [ ] Expired tokens are rejected

### Cross-Platform Consistency
- [ ] Both SDKs accept same JWT tokens
- [ ] Both SDKs return same user structure
- [ ] Both SDKs enforce roles identically
- [ ] Error responses are consistent
- [ ] Configuration options are equivalent

## Next Steps

After successful testing:

1. ✅ Mark all checklist items complete
2. 📝 Document any issues found
3. 🔧 Fix any bugs discovered
4. 🚀 Proceed with publication to NuGet.org and npm
5. 🏷️ Create Git tags for v1.0.0 release
6. 📦 Create GitHub Release

## Notes

- Both test apps use the same configuration (ClientId, JwtSecret, etc.)
- Tokens must use HS256 algorithm
- Tokens must include "aud" claim matching ClientId
- Tokens must include "exp" claim (not expired)
- Role claim can be string or array of strings
- Portal backend must be running for `/validate` endpoint tests
