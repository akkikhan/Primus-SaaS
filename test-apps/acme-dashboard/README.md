# 🏦 Acme Financial Dashboard

**Example application demonstrating Primus IdentityValidator integration with Azure AD**

This is a reference implementation showing how to integrate the Primus IdentityValidator SDK with Azure Active Directory authentication.

---

## 🎯 What This Demonstrates

✅ **Correct Azure AD Integration** (per ChatGPT architecture)  
✅ **Direct token validation** (no portal in runtime path)  
✅ **Optional analytics reporting** to Primus Portal  
✅ **Simple, production-ready setup**

---

## 🏗️ Architecture

```
User → Azure AD Login → Azure AD Token
  ↓
Frontend (MSAL) → GET /api/revenue-stats
  Authorization: Bearer <AzureADToken>
  ↓
Primus IdentityValidator Middleware
  ├─ Fetch JWKS from Azure AD (cached)
  ├─ Validate token signature
  ├─ Validate issuer, audience, expiry
  └─ Attach user info to request
  ↓
API Controller → Return protected data
```

**Key Point**: ⚠️ **Primus Portal is NOT in this flow**

---

## 📋 Prerequisites

### 1. Azure AD App Registration

You need an Azure AD application registered. If you don't have one:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** → **App registrations**
3. Click **New registration**
4. Fill in:
   - **Name**: Acme Dashboard API
   - **Supported account types**: Single tenant (or as needed)
   - **Redirect URI**: `http://localhost:3000/auth/callback` (for frontend)
5. Click **Register**

### 2. Get Your Credentials

After registration, note these values:

- **Tenant ID**: From Overview page
- **Client ID (Application ID)**: From Overview page

### 3. Configure Authentication

1. Go to **Authentication**
2. Enable **ID tokens** (for frontend MSAL)
3. Add redirect URIs for all environments
4. Save

---

## 🚀 Quick Start

### Step 1: Install Dependencies

```bash
npm install
```

### Step 2: Configure Environment

Copy the example environment file:

```bash
cp .env.example .env
```

Edit `.env` and add your Azure AD credentials:

```bash
# Azure AD Configuration
AZURE_AD_AUTHORITY=https://login.microsoftonline.com/YOUR-TENANT-ID
AZURE_AD_AUDIENCE=api://YOUR-CLIENT-ID

# Optional: Primus Portal tracking
PRIMUS_TRACKING_ID=PSP-CLI-711224
PRIMUS_PORTAL_URL=http://localhost:5267
```

### Step 3: Start the Server

```bash
node server.js
```

You should see:

```
🏦 ACME Financial Dashboard Server Starting...
✅ Primus Identity Validator configured
   - Authority: https://login.microsoftonline.com/YOUR-TENANT-ID
   - Tracking ID: PSP-CLI-711224
   - Portal URL: http://localhost:5267
🚀 Acme Dashboard running at http://localhost:3000
📝 API Endpoints:
   - GET /api/revenue-stats (protected)
   - GET /api/health (public)
```

### Step 4: Test

**Health Check** (no auth required):
```bash
curl http://localhost:3000/api/health
```

**Protected Endpoint** (requires Azure AD token):
```bash
# Get token from Azure AD first
curl http://localhost:3000/api/revenue-stats \
  -H "Authorization: Bearer YOUR_AZURE_AD_TOKEN"
```

---

## 🔧 Configuration Explained

### Required Configuration

```javascript
const PRIMUS_CONFIG = {
    // Required: Azure AD configuration
    defaultAuthority: 'https://login.microsoftonline.com/YOUR-TENANT-ID',
    allowedAudiences: ['api://YOUR-CLIENT-ID'],
};
```

**What these mean**:

- **defaultAuthority**: Your Azure AD tenant's authority URL
  - Single tenant: `https://login.microsoftonline.com/{tenantId}`
  - Multi-tenant: `https://login.microsoftonline.com/common`

- **allowedAudiences**: Array of allowed audience values in the token
  - Usually your API's Application ID: `api://{clientId}`

### Optional Configuration

```javascript
const PRIMUS_CONFIG = {
    // ... required config above ...
    
    // Optional: For portal analytics
    primusTrackingId: 'PSP-CLI-711224',  // From Primus Portal
    portalUrl: 'http://localhost:5267'   // Primus Portal URL
};
```

**What these mean**:

- **primusTrackingId**: Your application's tracking ID from Primus Portal
  - Used for analytics and usage reporting
  - **NOT** used for authentication
  - Optional - authentication works without it

- **portalUrl**: Primus Portal URL for analytics reporting
  - Used to send usage metrics (async, non-blocking)
  - **NOT** used for token validation
  - Optional - authentication works without it

---

## 🔐 How Authentication Works

### 1. User Login (Frontend)

```javascript
// Frontend uses MSAL to login with Azure AD
const msalConfig = {
    auth: {
        clientId: 'YOUR-CLIENT-ID',
        authority: 'https://login.microsoftonline.com/YOUR-TENANT-ID'
    }
};

const msalInstance = new msal.PublicClientApplication(msalConfig);

// User clicks "Login with Microsoft"
const loginResponse = await msalInstance.loginPopup({
    scopes: ['api://YOUR-CLIENT-ID/user_impersonation']
});

// Get access token
const tokenResponse = await msalInstance.acquireTokenSilent({
    scopes: ['api://YOUR-CLIENT-ID/user_impersonation']
});

const accessToken = tokenResponse.accessToken;
```

### 2. API Call (Frontend)

```javascript
// Frontend calls API with Azure AD token
const response = await fetch('http://localhost:3000/api/revenue-stats', {
    headers: {
        'Authorization': `Bearer ${accessToken}`
    }
});

const data = await response.json();
```

### 3. Token Validation (Backend - Automatic)

```javascript
// Primus middleware validates token automatically
app.get('/api/revenue-stats', primusAuth, (req, res) => {
    // If we reach here, token is valid
    // req.primusUser contains user info
    res.json({
        user: req.primusUser,
        data: { ... }
    });
});
```

**What the middleware does**:
1. Extracts token from `Authorization: Bearer` header
2. Fetches Azure AD public keys (JWKS) - cached
3. Validates token signature
4. Validates issuer (Azure AD)
5. Validates audience (your API)
6. Validates expiry
7. Extracts user claims
8. Attaches `req.primusUser` to request

**If validation fails**: Returns 401 Unauthorized

---

## 📊 What's in `req.primusUser`

After successful authentication, `req.primusUser` contains:

```javascript
{
    userId: "user-object-id-from-azure",
    email: "user@example.com",
    name: "John Doe",
    roles: ["User", "Admin"],  // From Azure AD roles
    additionalClaims: {
        // All other claims from the token
        tid: "tenant-id",
        aud: "api://your-client-id",
        iss: "https://login.microsoftonline.com/...",
        // ... etc
    }
}
```

---

## 🎯 Key Differences from Old Approach

### ❌ Old (WRONG) Approach

```javascript
// WRONG: Using portal credentials for authentication
const PRIMUS_CONFIG = {
    clientId: 'PSP-CLI-711224',  // ❌ Portal tracking ID
    clientSecret: 'psp_xxx...',   // ❌ Portal secret
    mode: 'AzureAd'
};

// WRONG: Two-token flow
app.post('/login-proxy', async (req, res) => {
    // ❌ Calls portal to validate token
    // ❌ Portal returns new token
    // ❌ Adds latency and complexity
});
```

### ✅ New (CORRECT) Approach

```javascript
// CORRECT: Using Azure AD credentials
const PRIMUS_CONFIG = {
    defaultAuthority: 'https://login.microsoftonline.com/YOUR-TENANT-ID',
    allowedAudiences: ['api://YOUR-CLIENT-ID'],
    
    // Optional: tracking only
    primusTrackingId: 'PSP-CLI-711224'
};

// CORRECT: Direct token validation
// No login-proxy needed
// SDK validates Azure AD tokens locally
```

---

## 🧪 Testing

### Test with Azure CLI

```bash
# Login to Azure
az login

# Get access token
az account get-access-token \
  --resource api://YOUR-CLIENT-ID \
  --query accessToken \
  --output tsv

# Use token to call API
curl http://localhost:3000/api/revenue-stats \
  -H "Authorization: Bearer $(az account get-access-token --resource api://YOUR-CLIENT-ID --query accessToken --output tsv)"
```

### Test with Postman

1. Create new request: `GET http://localhost:3000/api/revenue-stats`
2. Go to **Authorization** tab
3. Select **OAuth 2.0**
4. Configure:
   - **Grant Type**: Authorization Code
   - **Auth URL**: `https://login.microsoftonline.com/YOUR-TENANT-ID/oauth2/v2.0/authorize`
   - **Access Token URL**: `https://login.microsoftonline.com/YOUR-TENANT-ID/oauth2/v2.0/token`
   - **Client ID**: YOUR-CLIENT-ID
   - **Scope**: `api://YOUR-CLIENT-ID/user_impersonation`
5. Click **Get New Access Token**
6. Send request

---

## 🐛 Troubleshooting

### Error: "Invalid token signature"

**Cause**: Token was not issued by Azure AD or is corrupted

**Fix**: 
- Verify `defaultAuthority` matches your Azure AD tenant
- Ensure token is fresh (not expired)
- Check token was obtained for correct audience

### Error: "Audience mismatch"

**Cause**: Token's `aud` claim doesn't match `allowedAudiences`

**Fix**:
- Verify `allowedAudiences` includes your API's Application ID
- When getting token, use correct scope: `api://YOUR-CLIENT-ID/user_impersonation`

### Error: "Issuer mismatch"

**Cause**: Token's `iss` claim doesn't match expected issuer

**Fix**:
- Verify `defaultAuthority` is correct
- For single tenant: `https://login.microsoftonline.com/YOUR-TENANT-ID`
- For multi-tenant: `https://login.microsoftonline.com/common`

### Authentication works but no analytics in portal

**This is normal!** Analytics is optional and non-blocking.

To enable:
1. Ensure `primusTrackingId` is set
2. Ensure `portalUrl` is correct
3. Check portal is running
4. SDK reports usage every 5 minutes (async)

---

## 📚 Related Documentation

- [Primus Platform Overview](../PRIMUS_PLATFORM_OVERVIEW.md)
- [Implementation Plan](../IMPLEMENTATION_PLAN_FINAL.md)
- [Integration Guide](../INTEGRATION_GUIDE.md)
- [Azure AD Setup Guide](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app)

---

## 🎯 Summary

**This example demonstrates**:

✅ **Correct Azure AD integration** per ChatGPT architecture  
✅ **Direct token validation** (no portal in runtime path)  
✅ **PrimusClientId used for tracking only** (not authentication)  
✅ **Optional analytics** (non-blocking)  
✅ **Production-ready setup**

**Key Principle**: 
> "Portal is for management. SDK validates tokens locally. Portal is NOT in the authentication path."

---

**Questions?** See [IMPLEMENTATION_PLAN_FINAL.md](../IMPLEMENTATION_PLAN_FINAL.md) for detailed architecture and flow explanations.
