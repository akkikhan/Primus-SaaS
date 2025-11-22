# 🚀 Acme Dashboard - Complete Setup Guide

**Azure AD Authentication with Primus IdentityValidator**

This guide will walk you through setting up the Acme Financial Dashboard with Azure AD authentication from scratch.

---

## 📋 Prerequisites

Before you begin, ensure you have:

- ✅ Node.js 16+ installed
- ✅ An Azure account (free tier works)
- ✅ Access to Primus Portal
- ✅ Basic knowledge of JavaScript and Azure

---

## 🔧 Part 1: Azure AD Setup

### Step 1: Register Application in Azure Portal

1. **Go to Azure Portal**
   - Visit: https://portal.azure.com
   - Sign in with your Azure account

2. **Navigate to Azure Active Directory**
   - Click "Azure Active Directory" in the left sidebar
   - Or search for "Azure Active Directory" in the top search bar

3. **Register New Application**
   - Click "App registrations" in the left sidebar
   - Click "+ New registration"

4. **Fill in Registration Form**
   ```
   Name: Acme Dashboard
   Supported account types: Accounts in this organizational directory only (Single tenant)
   Redirect URI: 
     - Type: Single-page application (SPA)
     - URL: http://localhost:3000
   ```

5. **Click "Register"**

### Step 2: Get Your Credentials

After registration, you'll see the Overview page. **Copy these values**:

```
Application (client) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
Directory (tenant) ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

**⚠️ IMPORTANT: Save these values! You'll need them in the next steps.**

### Step 3: Configure Authentication

1. **Go to Authentication**
   - Click "Authentication" in the left sidebar

2. **Enable Tokens**
   - Under "Implicit grant and hybrid flows", check:
     - ☑️ ID tokens (used for sign-in)
     - ☑️ Access tokens (used for API calls)

3. **Add Redirect URIs**
   - Single-page application:
     - `http://localhost:3000`
     - `http://localhost:3000/auth/callback`

4. **Click "Save"**

### Step 4: Expose an API (For Backend Validation)

1. **Go to "Expose an API"**
   - Click "Expose an API" in the left sidebar

2. **Set Application ID URI**
   - Click "Set" next to Application ID URI
   - Use default: `api://YOUR-CLIENT-ID`
   - Click "Save"

3. **Add a Scope**
   - Click "+ Add a scope"
   - Fill in:
     ```
     Scope name: user_impersonation
     Who can consent: Admins and users
     Admin consent display name: Access Acme Dashboard API
     Admin consent description: Allows the app to access Acme Dashboard API as the signed-in user
     User consent display name: Access Acme Dashboard API
     User consent description: Allows the app to access Acme Dashboard API on your behalf
     State: Enabled
     ```
   - Click "Add scope"

**✅ Azure AD Setup Complete!**

---

## 🏗️ Part 2: Primus Portal Setup

### Step 1: Register in Primus Portal

1. **Go to Primus Portal**
   - Visit: http://localhost:5173
   - Sign up or log in

2. **Create Application**
   - Click "Applications" → "Create Application"
   - Fill in:
     ```
     Name: Acme Financial Dashboard
     Stack: Node.js
     Description: Financial reporting dashboard with Azure AD authentication
     ```
   - Click "Create"

3. **Note Your PrimusClientId**
   - After creation, you'll see: `PSP-CLI-XXXXXX`
   - **Save this value!**

### Step 2: Assign IdentityValidator Module

1. **Assign Module**
   - Click "Assign Module" on your application
   - Select: IdentityValidator v1.0.0
   - Click "Assign"

2. **View Documentation**
   - Click "View Documentation"
   - You'll see generated integration code
   - Keep this page open for reference

**✅ Primus Portal Setup Complete!**

---

## 💻 Part 3: Configure Acme Dashboard

### Step 1: Install Dependencies

```bash
cd test-apps/acme-dashboard
npm install
```

### Step 2: Configure Environment Variables

1. **Copy the example file**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` with your credentials**
   ```bash
   # Azure AD Configuration
   AZURE_AD_AUTHORITY=https://login.microsoftonline.com/YOUR-TENANT-ID
   AZURE_AD_AUDIENCE=api://YOUR-CLIENT-ID

   # Primus Portal Configuration (optional - for analytics)
   PRIMUS_TRACKING_ID=PSP-CLI-XXXXXX
   PRIMUS_PORTAL_URL=http://localhost:5267
   ```

   **Replace**:
   - `YOUR-TENANT-ID` with your Azure AD Tenant ID
   - `YOUR-CLIENT-ID` with your Azure AD Client ID
   - `PSP-CLI-XXXXXX` with your PrimusClientId from portal

### Step 3: Configure Frontend (MSAL)

Edit `public/js/msal-config.js`:

```javascript
const msalConfig = {
    auth: {
        clientId: "YOUR-AZURE-CLIENT-ID", // Replace with your Client ID
        authority: "https://login.microsoftonline.com/YOUR-TENANT-ID", // Replace with your Tenant ID
        redirectUri: "http://localhost:3000",
    },
    cache: {
        cacheLocation: "sessionStorage",
        storeAuthStateInCookie: false,
    }
};

const tokenRequest = {
    scopes: [
        "api://YOUR-AZURE-CLIENT-ID/user_impersonation" // Replace with your Client ID
    ]
};
```

**Replace**:
- `YOUR-AZURE-CLIENT-ID` with your Azure AD Client ID (3 places)
- `YOUR-TENANT-ID` with your Azure AD Tenant ID

**✅ Configuration Complete!**

---

## 🚀 Part 4: Run the Application

### Step 1: Start the Server

```bash
node server.js
```

You should see:

```
🏦 ACME Financial Dashboard Server Starting...
✅ Primus Identity Validator configured
   - Authority: https://login.microsoftonline.com/YOUR-TENANT-ID
   - Tracking ID: PSP-CLI-XXXXXX
   - Portal URL: http://localhost:5267
🚀 Acme Dashboard running at http://localhost:3000
📝 API Endpoints:
   - GET /api/revenue-stats (protected)
   - GET /api/health (public)
```

### Step 2: Open in Browser

1. **Open**: http://localhost:3000

2. **You should see**:
   - Login screen with "Sign in with Microsoft" button
   - Acme Corp branding

3. **Click "Sign in with Microsoft"**

4. **Login Flow**:
   - Popup window opens
   - Redirects to Microsoft login
   - Enter your Azure AD credentials
   - Grant consent (first time only)
   - Popup closes
   - Dashboard loads

5. **Dashboard Shows**:
   - Your name and email
   - Financial metrics (revenue, users, etc.)
   - Authentication details
   - Technology stack

**✅ Application Running!**

---

## 🧪 Part 5: Test the Flow

### Test 1: Health Check (No Auth Required)

```bash
curl http://localhost:3000/api/health
```

**Expected Response**:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-22T06:00:00.000Z"
}
```

### Test 2: Protected Endpoint (Auth Required)

1. **Login to the dashboard** (http://localhost:3000)
2. **Open browser console** (F12)
3. **Run this code**:

```javascript
// Get access token from MSAL
const accounts = msalInstance.getAllAccounts();
const response = await msalInstance.acquireTokenSilent({
    scopes: ["api://YOUR-CLIENT-ID/user_impersonation"],
    account: accounts[0]
});

// Call protected API
const apiResponse = await fetch('/api/revenue-stats', {
    headers: {
        'Authorization': `Bearer ${response.accessToken}`
    }
});

const data = await apiResponse.json();
console.log('API Response:', data);
```

**Expected Response**:
```json
{
  "company": "Acme Corp",
  "revenue": "$4,250,000",
  "growth": "+125%",
  "activeUsers": 14500,
  "lastUpdated": "2025-11-22T06:00:00.000Z",
  "user": {
    "userId": "...",
    "email": "you@example.com",
    "name": "Your Name",
    ...
  }
}
```

### Test 3: Token Validation (Backend)

**Check server logs** after making API call:

```
✅ Token validated successfully
   - User: you@example.com
   - Issuer: https://login.microsoftonline.com/YOUR-TENANT-ID/v2.0
   - Audience: api://YOUR-CLIENT-ID
   - Validation: Local (no portal call)
```

**✅ All Tests Passing!**

---

## 🔍 Troubleshooting

### Issue: "Login popup blocked"

**Solution**: Allow popups for localhost in your browser settings

### Issue: "Invalid redirect URI"

**Solution**: 
1. Check Azure AD → Authentication → Redirect URIs
2. Ensure `http://localhost:3000` is listed
3. Ensure type is "Single-page application (SPA)"

### Issue: "Audience mismatch"

**Solution**:
1. Check `.env` file: `AZURE_AD_AUDIENCE` should be `api://YOUR-CLIENT-ID`
2. Check `msal-config.js`: `scopes` should include `api://YOUR-CLIENT-ID/user_impersonation`

### Issue: "Token validation failed"

**Solution**:
1. Check server logs for specific error
2. Verify `AZURE_AD_AUTHORITY` in `.env` matches your tenant
3. Verify `AZURE_AD_AUDIENCE` matches your client ID

### Issue: "CORS error"

**Solution**: Frontend and backend must run on same origin (localhost:3000)

---

## 📊 Understanding the Flow

### Complete Authentication Flow

```
1. User clicks "Sign in with Microsoft"
   ↓
2. MSAL redirects to Azure AD
   ↓
3. User enters credentials in Azure AD
   ↓
4. Azure AD validates credentials
   ↓
5. Azure AD redirects back with tokens
   - id_token (for UI/profile)
   - access_token (for API calls)
   ↓
6. MSAL stores tokens in memory
   ↓
7. Frontend makes API call
   GET /api/revenue-stats
   Authorization: Bearer <access_token>
   ↓
8. Primus IdentityValidator middleware (in backend)
   ├─ Extracts token from header
   ├─ Fetches JWKS from Azure AD (cached)
   ├─ Validates signature using public key
   ├─ Validates issuer, audience, expiry
   ├─ Extracts user claims
   └─ Attaches req.primusUser
   ↓
9. API controller executes
   - Accesses req.primusUser
   - Returns protected data
   ↓
10. Frontend displays data
```

### What Primus Portal Does

✅ **Portal IS used for**:
- Application registration
- Module assignment
- Documentation generation
- PrimusClientId tracking
- (Optional) Analytics collection

❌ **Portal is NOT used for**:
- Token validation (SDK does locally)
- User authentication (Azure AD does)
- Runtime API calls (direct to your app)

---

## 🎯 Success Criteria

You've successfully set up the application when:

- [x] Can login with Microsoft account
- [x] Dashboard loads after login
- [x] Protected API returns data
- [x] Server logs show token validation
- [x] No errors in browser console
- [x] Logout works correctly

---

## 📚 Next Steps

1. **Customize the Dashboard**
   - Add your own API endpoints
   - Modify the UI
   - Add more features

2. **Deploy to Production**
   - Update redirect URIs in Azure AD
   - Update environment variables
   - Deploy backend and frontend

3. **Add More Modules**
   - Explore other Primus modules
   - Add logging, telemetry, etc.

---

## 🆘 Need Help?

- **Azure AD Issues**: Check [Azure AD documentation](https://docs.microsoft.com/azure/active-directory/)
- **MSAL Issues**: Check [MSAL.js documentation](https://github.com/AzureAD/microsoft-authentication-library-for-js)
- **Primus Issues**: Check `IMPLEMENTATION_PLAN_FINAL.md` or `END_TO_END_FLOW_VERIFICATION.md`

---

**Congratulations! You've successfully set up Azure AD authentication with Primus IdentityValidator!** 🎉
