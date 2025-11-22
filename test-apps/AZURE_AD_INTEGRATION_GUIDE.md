# 🔐 Azure AD Integration Guide for Acme Dashboard

**Last Updated**: November 22, 2025  
**Purpose**: Complete guide to test Azure AD authentication with Primus SaaS using real credentials

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Understanding the Flow](#understanding-the-flow)
4. [Azure AD Setup](#azure-ad-setup)
5. [Primus Portal Configuration](#primus-portal-configuration)
6. [Acme Dashboard Integration](#acme-dashboard-integration)
7. [Testing with Azure CLI](#testing-with-azure-cli)
8. [Frontend Changes](#frontend-changes)
9. [Complete Testing Flow](#complete-testing-flow)
10. [Troubleshooting](#troubleshooting)

---

## Overview

This guide walks you through integrating **Azure AD authentication** into your Acme Dashboard using the Primus SaaS platform. You'll learn how to:

- Configure Azure AD app registration
- Set up Primus Portal for Azure AD mode
- Modify Acme Dashboard to use Azure AD tokens
- Test the complete flow using Azure CLI and browser

---

## Prerequisites

### What You Need

✅ **Azure AD Credentials**:
- Azure AD Tenant ID
- Azure AD Application (Client) ID  
- User credentials (email/password) for testing

✅ **Primus Credentials** (Already have):
- Primus Client ID: `PSP-CLI-711224`
- Primus Client Secret: `psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI`

✅ **Tools Installed**:
- Azure CLI (`az` command)
- Node.js 16+
- Running Primus Portal backend (localhost:5267)

---

## Understanding the Flow

### 🔄 Complete Authentication Flow

```
┌─────────────┐         ┌──────────────┐         ┌─────────────┐         ┌──────────────┐
│   Browser   │         │    Acme      │         │   Primus    │         │  Azure AD    │
│   (User)    │         │  Dashboard   │         │   Portal    │         │  (Microsoft) │
└──────┬──────┘         └──────┬───────┘         └──────┬──────┘         └──────┬───────┘
       │                       │                        │                        │
       │ 1. Click "Login"      │                        │                        │
       ├──────────────────────►│                        │                        │
       │                       │                        │                        │
       │ 2. Redirect to Azure AD                        │                        │
       ├───────────────────────┴────────────────────────┴───────────────────────►│
       │                                                                          │
       │ 3. User enters Azure credentials                                        │
       ├─────────────────────────────────────────────────────────────────────────►│
       │                                                                          │
       │ 4. Azure returns ID Token                                               │
       │◄─────────────────────────────────────────────────────────────────────────┤
       │                       │                        │                        │
       │ 5. Send ID Token to Primus Portal              │                        │
       ├──────────────────────►│───────────────────────►│                        │
       │                       │                        │                        │
       │                       │ 6. Validate with Azure AD                       │
       │                       │                        ├───────────────────────►│
       │                       │                        │                        │
       │                       │ 7. Validation Success  │                        │
       │                       │                        │◄───────────────────────┤
       │                       │                        │                        │
       │                       │ 8. Return Portal JWT   │                        │
       │                       │◄───────────────────────┤                        │
       │                       │                        │                        │
       │ 9. Store JWT & Access Dashboard                │                        │
       │◄──────────────────────┤                        │                        │
       │                       │                        │                        │
       │ 10. API Request with JWT                       │                        │
       ├──────────────────────►│                        │                        │
       │                       │                        │                        │
       │                       │ 11. Validate JWT (Azure AD mode)                │
       │                       │ - Fetch Azure AD signing keys                   │
       │                       │ - Verify signature                              │
       │                       │ - Check tenant/audience                         │
       │                       │                        │                        │
       │ 12. Return Protected Data                      │                        │
       │◄──────────────────────┤                        │                        │
```

### Key Points:

1. **Azure AD** authenticates the user and issues an **ID Token**
2. **Primus Portal** validates the Azure AD token and creates a **session JWT**
3. **Acme Dashboard** uses the Primus SDK to validate requests with Azure AD tokens
4. The SDK automatically fetches Azure AD public keys to verify token signatures

---

## Azure AD Setup

### Step 1: Register Application in Azure Portal

1. **Go to Azure Portal**: https://portal.azure.com
2. Navigate to **Azure Active Directory** → **App registrations**
3. Click **"New registration"**

**Configuration**:
```
Name: Acme Dashboard (or your app name)
Supported account types: Accounts in this organizational directory only
Redirect URI: 
  - Type: Single-page application (SPA)
  - URI: http://localhost:3000/auth/callback
```

4. Click **"Register"**

### Step 2: Note Your Azure AD Credentials

After registration, copy these values:

```
Tenant ID: cbd15a9b-cd52-4ccc-916a-00e2edb13043 (example)
Client ID (Application ID): c28b195b-8396-42e6-bc6f-7773736dfa40 (example)
```

### Step 3: Configure Authentication

1. Go to **Authentication** in your app registration
2. Under **Implicit grant and hybrid flows**, enable:
   - ✅ **ID tokens** (used for user sign-in)
3. Click **Save**

### Step 4: API Permissions (Optional)

For basic authentication, default permissions are sufficient:
- Microsoft Graph → User.Read (already added)

---

## Primus Portal Configuration

### Update Backend Configuration

**File**: `c:\Users\aakib\Primus SaaS\portal\backend\appsettings.json`

```json
{
  "AzureAd": {
    "TenantId": "YOUR_AZURE_TENANT_ID",
    "ClientId": "YOUR_AZURE_CLIENT_ID",
    "Audience": "YOUR_AZURE_CLIENT_ID"
  }
}
```

**Example**:
```json
{
  "AzureAd": {
    "TenantId": "cbd15a9b-cd52-4ccc-916a-00e2edb13043",
    "ClientId": "c28b195b-8396-42e6-bc6f-7773736dfa40",
    "Audience": "c28b195b-8396-42e6-bc6f-7773736dfa40"
  }
}
```

### Restart Primus Portal Backend

```powershell
# Stop the current backend (Ctrl+C in the terminal)
# Then restart:
cd "c:\Users\aakib\Primus SaaS\portal\backend"
dotnet run
```

You should see:
```
[Azure AD Config] Tenant ID: cbd15a9b-cd52-4ccc-916a-00e2edb13043
[Azure AD Config] Client ID: c28b195b-8396-42e6-bc6f-7773736dfa40
[Azure AD Config] Authority: https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/v2.0
```

---

## Acme Dashboard Integration

### Current Configuration

Your Acme Dashboard is already configured for Azure AD mode:

**File**: `c:\Users\aakib\Primus SaaS\test-apps\acme-dashboard\server.js`

```javascript
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',
    clientId: 'PSP-CLI-711224',           // Primus Client ID
    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'AzureAd',                      // ✅ Already set to Azure AD
    tenantId: 'common'                    // ⚠️ Need to update this
};
```

### Required Changes

**Update `tenantId`** to match your Azure AD tenant:

```javascript
const PRIMUS_CONFIG = {
    portalUrl: 'http://localhost:5267',
    clientId: 'PSP-CLI-711224',
    clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
    mode: 'AzureAd',
    tenantId: 'cbd15a9b-cd52-4ccc-916a-00e2edb13043'  // ✅ Your actual tenant ID
};
```

### Restart Acme Dashboard

```powershell
# Stop the current server (Ctrl+C)
# Then restart:
cd "c:\Users\aakib\Primus SaaS\test-apps\acme-dashboard"
node server.js
```

---

## Testing with Azure CLI

### Step 1: Install Azure CLI

If not already installed:
```powershell
winget install Microsoft.AzureCLI
```

### Step 2: Login to Azure

```powershell
az login
```

This will open a browser for authentication.

### Step 3: Get an Azure AD Token

```powershell
# Get access token for your application
az account get-access-token --resource YOUR_AZURE_CLIENT_ID
```

**Example**:
```powershell
az account get-access-token --resource c28b195b-8396-42e6-bc6f-7773736dfa40
```

**Output**:
```json
{
  "accessToken": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "expiresOn": "2025-11-22 10:30:00.000000",
  "subscription": "...",
  "tenant": "cbd15a9b-cd52-4ccc-916a-00e2edb13043",
  "tokenType": "Bearer"
}
```

### Step 4: Test Authentication with Primus Portal

```powershell
# Save the token
$azureToken = "eyJ0eXAiOiJKV1QiLCJhbGc..."

# Test Azure login endpoint
$body = @{
    idToken = $azureToken
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5267/api/auth/azure" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

**Expected Response**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "id": 1,
  "email": "user@yourdomain.com",
  "role": "Admin"
}
```

### Step 5: Test Protected Endpoint

```powershell
# Use the token from previous step
$primusToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Test Acme Dashboard API
Invoke-RestMethod -Uri "http://localhost:3000/api/revenue-stats" `
    -Headers @{
        "Authorization" = "Bearer $primusToken"
    }
```

**Expected Response**:
```json
{
  "company": "Acme Corp",
  "revenue": "$4,250,000",
  "growth": "+125%",
  "activeUsers": 14500,
  "lastUpdated": "2025-11-22T04:56:42.123Z",
  "user": {
    "userId": "...",
    "email": "user@yourdomain.com",
    "name": "...",
    "roles": ["Admin"]
  }
}
```

---

## Frontend Changes

### Update Login Flow for Azure AD

**File**: `c:\Users\aakib\Primus SaaS\test-apps\acme-dashboard\public\js\auth.js`

Add Azure AD authentication:

```javascript
// Azure AD Configuration
const AZURE_CONFIG = {
    tenantId: 'cbd15a9b-cd52-4ccc-916a-00e2edb13043',
    clientId: 'c28b195b-8396-42e6-bc6f-7773736dfa40',
    redirectUri: 'http://localhost:3000/auth/callback',
    scope: 'openid profile email'
};

// Azure AD Login
async function loginWithAzure() {
    const authUrl = `https://login.microsoftonline.com/${AZURE_CONFIG.tenantId}/oauth2/v2.0/authorize?` +
        `client_id=${AZURE_CONFIG.clientId}` +
        `&response_type=id_token` +
        `&redirect_uri=${encodeURIComponent(AZURE_CONFIG.redirectUri)}` +
        `&scope=${encodeURIComponent(AZURE_CONFIG.scope)}` +
        `&response_mode=fragment` +
        `&nonce=${generateNonce()}`;
    
    window.location.href = authUrl;
}

// Handle Azure AD Callback
async function handleAzureCallback() {
    const hash = window.location.hash.substring(1);
    const params = new URLSearchParams(hash);
    const idToken = params.get('id_token');
    
    if (!idToken) {
        throw new Error('No ID token received from Azure AD');
    }
    
    // Send ID token to Primus Portal for validation
    const response = await fetch('http://localhost:5267/api/auth/azure', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ idToken })
    });
    
    if (!response.ok) {
        throw new Error('Azure AD authentication failed');
    }
    
    const data = await response.json();
    
    // Store Primus JWT token
    localStorage.setItem('authToken', data.token);
    localStorage.setItem('userEmail', data.email);
    
    // Redirect to dashboard
    window.location.href = '/dashboard.html';
}

function generateNonce() {
    return Math.random().toString(36).substring(2, 15);
}
```

### Update Login Page

**File**: `c:\Users\aakib\Primus SaaS\test-apps\acme-dashboard\public\index.html`

Add Azure AD login button:

```html
<button onclick="loginWithAzure()" class="azure-login-btn">
    <img src="https://docs.microsoft.com/en-us/azure/active-directory/develop/media/howto-add-branding-in-azure-ad-apps/ms-symbollockup_mssymbol_19.svg" alt="Microsoft">
    Sign in with Microsoft
</button>
```

### Create Callback Page

**File**: `c:\Users\aakib\Primus SaaS\test-apps\acme-dashboard\public\auth\callback.html`

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Authenticating...</title>
</head>
<body>
    <div style="text-align: center; padding: 50px;">
        <h2>Authenticating with Azure AD...</h2>
        <p>Please wait while we complete your sign-in.</p>
    </div>
    
    <script src="../js/auth.js"></script>
    <script>
        handleAzureCallback().catch(error => {
            alert('Authentication failed: ' + error.message);
            window.location.href = '/';
        });
    </script>
</body>
</html>
```

---

## Complete Testing Flow

### Test Scenario 1: Azure CLI Token

```powershell
# 1. Get Azure AD token
$token = (az account get-access-token --resource YOUR_CLIENT_ID | ConvertFrom-Json).accessToken

# 2. Login to Primus Portal
$body = @{ idToken = $token } | ConvertTo-Json
$primusAuth = Invoke-RestMethod -Uri "http://localhost:5267/api/auth/azure" -Method POST -Body $body -ContentType "application/json"

# 3. Test Acme Dashboard
Invoke-RestMethod -Uri "http://localhost:3000/api/revenue-stats" -Headers @{ "Authorization" = "Bearer $($primusAuth.token)" }
```

### Test Scenario 2: Browser Flow

1. **Navigate to**: http://localhost:3000
2. **Click**: "Sign in with Microsoft"
3. **Enter**: Azure AD credentials
4. **Verify**: Redirected to dashboard
5. **Check**: Revenue stats load successfully

### Test Scenario 3: Hybrid Mode

Update Acme Dashboard to use Hybrid mode:

```javascript
const PRIMUS_CONFIG = {
    // ... other config
    mode: 'Hybrid',  // Try Azure AD first, fallback to Local
    tenantId: 'cbd15a9b-cd52-4ccc-916a-00e2edb13043'
};
```

This allows both Azure AD tokens AND local JWT tokens to work.

---

## Troubleshooting

### Issue 1: "Tenant ID not configured"

**Cause**: Azure AD settings missing in Primus Portal

**Solution**:
```json
// portal/backend/appsettings.json
{
  "AzureAd": {
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID",
    "Audience": "YOUR_CLIENT_ID"
  }
}
```

### Issue 2: "Invalid Azure AD token"

**Cause**: Token audience mismatch

**Solution**: Ensure the token is requested for your Azure AD app:
```powershell
az account get-access-token --resource YOUR_AZURE_CLIENT_ID
```

### Issue 3: "CORS error"

**Cause**: Azure AD redirect blocked by CORS

**Solution**: Add redirect URI in Azure Portal:
- Azure AD → App registrations → Your app → Authentication
- Add: `http://localhost:3000/auth/callback`

### Issue 4: "Token signature verification failed"

**Cause**: Azure AD public keys not fetched

**Solution**: Check Primus Portal logs for:
```
[Azure Login] Fetching Azure AD signing keys...
[Azure Login] Token validated successfully
```

If missing, verify internet connectivity and Azure AD endpoint accessibility.

### Issue 5: "User not found in database"

**Cause**: Azure AD user not registered in Primus Portal

**Solution**: 
1. First login creates user automatically
2. Or manually create user in portal with same email

---

## Configuration Summary

### Environment Variables Needed

**Primus Portal** (`appsettings.json`):
```json
{
  "AzureAd": {
    "TenantId": "YOUR_AZURE_TENANT_ID",
    "ClientId": "YOUR_AZURE_CLIENT_ID",
    "Audience": "YOUR_AZURE_CLIENT_ID"
  }
}
```

**Acme Dashboard** (`server.js`):
```javascript
{
  portalUrl: 'http://localhost:5267',
  clientId: 'PSP-CLI-711224',
  clientSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
  jwtSecret: 'psp_h9lckDU8Kk70PsC5u9b9uFHtIKBm62bXWO6NVqWtWPI',
  mode: 'AzureAd',
  tenantId: 'YOUR_AZURE_TENANT_ID'
}
```

**Frontend** (`auth.js`):
```javascript
{
  tenantId: 'YOUR_AZURE_TENANT_ID',
  clientId: 'YOUR_AZURE_CLIENT_ID',
  redirectUri: 'http://localhost:3000/auth/callback',
  scope: 'openid profile email'
}
```

---

## Quick Start Checklist

- [ ] Azure AD app registered
- [ ] Redirect URI configured (`http://localhost:3000/auth/callback`)
- [ ] ID tokens enabled in Azure AD app
- [ ] Primus Portal `appsettings.json` updated with Azure AD config
- [ ] Primus Portal backend restarted
- [ ] Acme Dashboard `server.js` updated with tenant ID
- [ ] Acme Dashboard server restarted
- [ ] Azure CLI installed and logged in
- [ ] Test token obtained via `az account get-access-token`
- [ ] Successfully authenticated with Primus Portal
- [ ] Protected API endpoint returns data

---

## Next Steps

1. **Test with real Azure AD users** in your organization
2. **Implement role mapping** from Azure AD groups to Primus roles
3. **Add refresh token support** for long-lived sessions
4. **Configure production redirect URIs** for deployment
5. **Set up monitoring** for authentication failures

---

**Questions?** Check the Primus Portal logs and Azure AD sign-in logs for detailed error messages.

**Happy Testing! 🚀**
