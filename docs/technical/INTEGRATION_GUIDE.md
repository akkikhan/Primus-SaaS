# 🚀 Real-Life Integration Guide - Primus SaaS Platform

**Last Updated**: November 22, 2025  
**Version**: 1.1.0

---

## 🆕 Release Notes (v1.1.0)

- **Multi-Issuer Support**: Now supports configuring multiple identity providers (e.g., Azure AD + Local) simultaneously.
- **Simplified Configuration**: Removed `mode` and `portalUrl`. Use the `issuers` array instead.
- **Local Auth**: Enhanced support for self-hosted Local Identity Providers.
- **Breaking Changes**: `PrimusIdentityOptions` structure has changed. See [Node.js Integration](#nodejs--express-integration) for details.

---

## 📋 Table of Contents

1. [Quick Start](#quick-start)
2. [Step-by-Step Integration](#step-by-step-integration)
3. [Node.js / Express Integration](#nodejs--express-integration)
4. [.NET / ASP.NET Core Integration](#net--aspnet-core-integration)
5. [Testing Your Integration](#testing-your-integration)
6. [Production Deployment](#production-deployment)
7. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Prerequisites

- Access to Primus SaaS Portal
- Admin account credentials
- Node.js 16+ OR .NET 7+ installed
- Basic knowledge of REST APIs

### Expected Time

**15 minutes** from portal registration to working protected API

---

## Step-by-Step Integration

### Step 1: Register Your Application in Portal

1. **Login to Primus Portal**
   -  Navigate to: `http://your-portal-url.com/login`
   - Use your admin credentials

2. **Create New Application**
   - Click "Applications" in sidebar
   - Click "Create Application"
   - Fill in details:
     ```
     Application Name: My Awesome App
     Stack: NodeJS (or DotNet)
     Description: Production API for...
     ```
   - Click "Submit"

3. **Save Your Credentials** ⚠️
   ```
   Client ID:     PSP-CLI-XXXXXX
   Client Secret: psp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   ```
   
   **CRITICAL**: Copy the Client Secret immediately. It's only shown once!

---

### Step 2: Choose Your SDK

#### For Node.js / Express

```bash
npm install @primus-saas/identity-validator
```

#### For .NET / ASP.NET Core

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

---

## Node.js / Express Integration

### Installation

```bash
npm install @primus-saas/identity-validator express
```

### Basic Implementation

**File: `server.js`**

```javascript
const express = require('express');
const { primusIdentityMiddleware, requireRoles } = require('primus-identity-validator');

const app = express();

// ✅ Step 1: Configure Primus Authentication
const primusAuth = primusIdentityMiddleware({
    issuers: [
        {
            name: 'LocalAuth',
            type: 'jwt',
            issuer: 'https://auth.local',
            secret: process.env.LOCAL_JWT_SECRET,
            audiences: ['api://my-app']
        },
        {
            name: 'AzureAD',
            type: 'oidc',
            authority: 'https://login.microsoftonline.com/common/v2.0',
            issuer: 'https://login.microsoftonline.com/{tenant-id}/v2.0',
            audiences: ['api://my-app']
        }
    ],
    clockSkew: 300
});

// ✅ Step 2: Create Public Endpoints (no authentication)
app.get('/api/public', (req, res) => {
    res.json({ message: 'Public data - no auth required' });
});

// ✅ Step 3: Create Protected Endpoints
app.get('/api/protected', primusAuth, (req, res) => {
    // Access authenticated user data
    const user = req.primusUser;
    
    res.json({
        message: 'Protected data',
        user: {
            id: user.userId,
            email: user.email,
            name: user.name,
            roles: user.roles
        }
    });
});

// ✅ Step 4: Create Role-Based Endpoints
app.get('/api/admin-only', primusAuth, requireRoles('Admin'), (req, res) => {
    res.json({ message: 'Admin-only data' });
});

app.listen(3000, () => {
    console.log('Server running on http://localhost:3000');
});
```

### Environment Variables

**File: `.env`**

```bash
PRIMUS_PORTAL_URL=https://portal.yourcompany.com
PRIMUS_CLIENT_ID=PSP-CLI-932655
PRIMUS_CLIENT_SECRET=psp_ppXmhBjdNlkj1cEqFRQJqo82p6mPGay5LFxmycgPd1k
NODE_ENV=production
```

### TypeScript Usage

```typescript
import express, { Request, Response } from 'express';
import { primusIdentityMiddleware, PrimusUser } from 'primus-identity-validator';

// Extend Express Request type
declare global {
    namespace Express {
        interface Request {
            primusUser?: PrimusUser;
        }
    }
}

const app = express();
const primusAuth = primusIdentityMiddleware({
    issuers: [
        {
            name: 'LocalAuth',
            type: 'jwt',
            issuer: 'https://auth.local',
            secret: process.env.LOCAL_JWT_SECRET!,
            audiences: ['api://my-app']
        }
    ]
});

app.get('/api/protected', primusAuth, (req: Request, res: Response) => {
    const user = req.primusUser!;
    res.json({ userId: user.userId, email: user.email });
});

app.listen(3000);
```

---

## .NET / ASP.NET Core Integration

### Installation

```bash
dotnet add package PrimusSaaS.Identity.Validator
```

### Basic Implementation

**File: `Program.cs`**

```csharp
using PrimusSaaS.Identity.Validator;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ✅ Step 1: Add Primus Identity Services
// ✅ Step 1: Add Primus Identity Services
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "LocalAuth",
            Type = IssuerType.Jwt,
            Issuer = "https://auth.local",
            Secret = builder.Configuration["Primus:LocalSecret"],
            Audiences = new List<string> { "api://my-app" }
        },
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Authority = "https://login.microsoftonline.com/common/v2.0",
            Issuer = "https://login.microsoftonline.com/{tenant-id}/v2.0",
            Audiences = new List<string> { "api://my-app" }
        }
    };
    
    // Optional: Configure for development
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.ValidateLifetime = true;
    options.ClockSkew = TimeSpan.FromMinutes(5);
});

// ✅ Step 2: Add Authorization
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// ✅ Step 3: Enable Authentication Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

**File: `appsettings.json`**

```json
{
  "Primus": {
    "LocalSecret": "your-local-jwt-secret-key-here",
    "AzureAd": {
      "TenantId": "your-tenant-id",
      "ClientId": "your-client-id"
    }
  }
}
```

**File: `Controllers/SecureController.cs`**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Identity.Validator;

[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    // ✅ Public endpoint (no authentication)
    [HttpGet("public")]
    public IActionResult GetPublic()
    {
        return Ok(new { message = "Public data" });
    }

    // ✅ Protected endpoint (requires valid token)
    [HttpGet("protected")]
    [Authorize]
    public IActionResult GetProtected()
    {
        var user = HttpContext.GetPrimusUser();
        
        return Ok(new
        {
            message = "Protected data",
            user = new
            {
                id = user?.UserId,
                email = user?.Email,
                name = user?.Name,
                roles = user?.Roles
            }
        });
    }

    // ✅ Admin-only endpoint
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdmin()
    {
        return Ok(new { message = "Admin-only data" });
    }
}
```

---

## Testing Your Integration

### 1. Get a Test Token

**Method A: From Portal** (Recommended for production)
1. Login to Primus Portal
2. Navigate to your application
3. Click "Generate Test Token"
4. Copy the JWT token

**Method B: Using HMAC** (For testing/development)

```javascript
// Node.js example
const crypto = require('crypto');

function generateTestToken(clientId, clientSecret, email = 'test@example.com') {
    const payload = {
        primusClientId: clientId,
        email: email,
        userId: '12345',
        role: 'user'
    };
    
    const message = `${clientId}|${email}|${payload.userId}|${payload.role}`;
    const signature = crypto
        .createHmac('sha256', clientSecret)
        .update(message)
        .digest('base64');
    
    payload.signature = signature;
    
    return Buffer.from(JSON.stringify(payload)).toString('base64');
}

const token = generateTestToken('PSP-CLI-932655', 'psp_ppXmhBjdNlkj...');
console.log(`Token: ${token}`);
```

### 2. Test Protected Endpoints

```bash
# Test without token (should fail with 401)
curl http://localhost:3000/api/protected

# Test with token (should succeed)
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
     http://localhost:3000/api/protected
```

### 3. Expected Responses

**Success (200)**:
```json
{
  "message": "Protected data",
  "user": {
    "id": "12345",
    "email": "test@example.com",
    "name": "Test User",
    "roles": ["user"]
  }
}
```

**Unauthorized (401)**:
```json
{
  "error": "No authorization token provided"
}
```

---

## Production Deployment

### Security Checklist

- [ ] **Store secrets in environment variables** (never in code)
- [ ] **Use HTTPS** in production
- [ ] **Rotate client secrets** regularly (every 90 days)
- [ ] **Enable token expiration validation**
- [ ] **Log failed authentication attempts**
- [ ] **Use role-based access control** (RBAC)
- [ ] **Set appropriate CORS policies**

### Environment Variables

#### Node.js (PM2)

**File: `ecosystem.config.js`**

```javascript
module.exports = {
    apps: [{
        name: 'my-app',
        script: './server.js',
        env_production: {
            NODE_ENV: 'production',
            PRIMUS_PORTAL_URL: 'https://portal.yourcompany.com',
            PRIMUS_CLIENT_ID: process.env.PRIMUS_CLIENT_ID,
            PRIMUS_CLIENT_SECRET: process.env.PRIMUS_CLIENT_SECRET
        }
    }]
};
```

#### .NET (Azure App Service)

```bash
az webapp config appsettings set \
  --name myapp \
  --resource-group mygroup \
  --settings \
    Primus__ClientId="PSP-CLI-XXXXXX" \
    Primus__ClientSecret="psp_xxxxxxxxx"
```

### Docker Deployment

**Dockerfile**:

```dockerfile
FROM node:20-alpine
WORKDIR /app
COPY package*.json ./
RUN npm ci --production
COPY . .
EXPOSE 3000
CMD ["node", "server.js"]
```

**docker-compose.yml**:

```yaml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "3000:3000"
    environment:
      - PRIMUS_PORTAL_URL=${PRIMUS_PORTAL_URL}
      - PRIMUS_CLIENT_ID=${PRIMUS_CLIENT_ID}
      - PRIMUS_CLIENT_SECRET=${PRIMUS_CLIENT_SECRET}
    restart: unless-stopped
```

---

## Troubleshooting

### Common Issues

#### 1. "No authorization token provided"

**Cause**: Missing Authorization header

**Solution**:
```bash
# ✅ Correct
curl -H "Authorization: Bearer YOUR_TOKEN" http://...

# ❌ Wrong
curl http://... # Missing header
```

#### 2. "Invalid signature"

**Cause**: Client secret mismatch

**Solution**:
- Verify Client ID and Secret match portal exactly
- Check for extra spaces or quotes in environment variables
- Regenerate secret in portal if needed

#### 3. "Token expired"

**Cause**: Token has exceeded its lifetime

**Solution**:
- Generate a new token
- Check server time synchronization
- Increase `clockSkew` for development:
  ```javascript
  primusIdentityMiddleware({
      // ...
      clockSkew: 300 // 5 minutes tolerance
  })
  ```

#### 4. "Module not found: primus-identity-validator"

**Cause**: SDK not installed

**Solution**:
```bash
# Node.js
npm install primus-identity-validator

# .NET
dotnet add package PrimusSaaS.Identity.Validator
```

#### 5. "CORS policy error"

**Cause**: Cross-origin requests blocked

**Solution** (Express):
```javascript
const cors = require('cors');
app.use(cors({
    origin: 'https://your-frontend.com',
    credentials: true
}));
```

---

## Additional Resources

### Documentation
- **Portal Documentation**: [View in Portal → Documentation]
- **Node.js SDK**: `/sdk/nodejs/primus-identity-validator/README.md`
- **.NET SDK**: `/sdk/dotnet/PrimusSaaS.Identity.Validator/README.md`

### Example Applications
-  **Node.js Demo**: `/test-apps/demo-app.js`
- **Integration Tests**: `/test-apps/e2e-test.js`

### Support
- **GitHub Issues**: https://github.com/akkikhan/Primus-SaaS/issues
- **Portal Support**: Contact your portal administrator

---

## ✅ Integration Checklist

Use this checklist to verify your integration:

- [ ] Created application in Primus Portal
- [ ] Saved Client ID and Client Secret
- [ ] Installed appropriate SDK (npm/NuGet)
- [ ] Configured middleware with credentials
- [ ] Created protected endpoints
- [ ] Tested with valid token (returns 200)
- [ ] Tested without token (returns 401)
- [ ] Implemented role-based access  (if needed)
- [ ] Stored secrets in environment variables
- [ ] Tested in staging environment
- [ ] Ready for production deployment

---

**Questions?** Check the troubleshooting section or consult your portal documentation.

**Happy coding! 🚀**
