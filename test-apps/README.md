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

---

## Azure AD Configuration (Task 6)

The test app now supports **dual-mode validation**:
- **Local Mode**: Symmetric key validation using JwtSecret (HS256)
- **Azure AD Mode**: JWKS-based validation using Azure AD public keys (RS256)

### Configuring Azure AD Mode

#### Step 1: Configure appsettings.Development.json

Update the `PrimusIdentity` section in `test-apps/dotnet-test-app/PrimusTest.Api/appsettings.Development.json`:

```json
{
  "PrimusIdentity": {
    "PortalUrl": "https://localhost:7001",
    "ClientId": "test-client-123",
    "ClientSecret": "test-secret-456",
    "Mode": "AzureAd",  // Change from "Local" to "AzureAd"
    "TenantId": "<YOUR_AZURE_AD_TENANT_ID>",  // Replace with your Azure AD tenant ID
    "JwksCacheTtl": 24  // Optional: Cache TTL in hours (default: 24)
  }
}
```

**Configuration Options:**
- `Mode`: `"Local"`, `"AzureAd"`, or `"Hybrid"` (default: `"Local"`)
- `TenantId`: Your Azure AD tenant ID (GUID or domain name) - **REQUIRED** for Azure AD mode
- `JwksCacheTtl`: JWKS cache duration in hours - **OPTIONAL** (default: 24 hours)

#### Step 2: Create Azure AD App Registration

1. **Navigate to Azure Portal**:
   - Go to [https://portal.azure.com](https://portal.azure.com)
   - Navigate to **Azure Active Directory** → **App registrations** → **New registration**

2. **Register Application**:
   - **Name**: `Primus SaaS Test App`
   - **Supported account types**: Choose based on your requirements
     - Single tenant: Accounts in this organizational directory only
     - Multi-tenant: Accounts in any organizational directory
   - **Redirect URI**: (Optional) Add if needed for your auth flow
   - Click **Register**

3. **Obtain Tenant ID**:
   - After registration, go to **Overview** tab
   - Copy the **Directory (tenant) ID** (GUID format: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)
   - Update `TenantId` in `appsettings.Development.json` with this value

4. **Configure API Permissions** (if needed):
   - Go to **API permissions** tab
   - Add permissions required by your app
   - Grant admin consent if necessary

5. **Expose an API** (optional):
   - Go to **Expose an API** tab
   - Add scopes if needed for your API

#### Step 3: Generate Azure AD Tokens

To test with real Azure AD tokens, use one of these methods:

**Option A: Using MSAL (Recommended for production)**

```powershell
# Install MSAL.PS module (one-time setup)
Install-Module -Name MSAL.PS -Scope CurrentUser

# Get access token
$token = Get-MsalToken `
    -ClientId "YOUR_CLIENT_ID" `
    -TenantId "YOUR_TENANT_ID" `
    -Scopes "https://graph.microsoft.com/.default"

# Use the token
$accessToken = $token.AccessToken
Write-Host "Token: $accessToken"
```

**Option B: Using Azure CLI**

```powershell
# Login to Azure
az login

# Get access token
$token = az account get-access-token --query accessToken -o tsv

# Use the token
Write-Host "Token: $token"
```

**Option C: Using Postman/OAuth Flow**

1. Configure OAuth 2.0 in Postman
2. Set **Auth URL**: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize`
3. Set **Access Token URL**: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token`
4. Set **Client ID** and **Scope**
5. Click **Get New Access Token**

#### Step 4: Test Azure AD Mode

**.NET Test App:**

```bash
# Start the test app
cd test-apps/dotnet-test-app/PrimusTest.Api
dotnet run

# Test protected endpoint with Azure AD token
curl https://localhost:7xxx/api/protected \
  -H "Authorization: Bearer YOUR_AZURE_AD_TOKEN"
```

**Expected Behavior:**
- ✅ App starts with Azure AD validation mode
- ✅ JWKS keys are cached from Azure AD (https://login.microsoftonline.com/{tenant}/discovery/keys)
- ✅ Valid Azure AD tokens (RS256) are accepted
- ✅ Invalid tokens are rejected with 401 Unauthorized
- ✅ Token validation uses public key from JWKS endpoint

#### Step 5: Switching Between Modes

**Switch to Local Mode:**
```json
{
  "PrimusIdentity": {
    "Mode": "Local",
    "JwtSecret": "test-jwt-secret-key-min-32-chars-required-for-hs256-algorithm"
    // TenantId not required in Local mode
  }
}
```

**Switch to Azure AD Mode:**
```json
{
  "PrimusIdentity": {
    "Mode": "AzureAd",
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    // JwtSecret not required in Azure AD mode
  }
}
```

**Switch to Hybrid Mode** (supports both Local and Azure AD tokens):
```json
{
  "PrimusIdentity": {
    "Mode": "Hybrid",
    "JwtSecret": "test-jwt-secret-key-min-32-chars-required-for-hs256-algorithm",
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  }
}
```

### Troubleshooting Azure AD Mode

**Error: "TenantId is required for Azure AD mode"**
- Ensure `TenantId` is set in `appsettings.Development.json`
- Verify the tenant ID is a valid GUID format

**Error: "Unable to retrieve JWKS keys"**
- Check internet connectivity
- Verify Azure AD endpoint is accessible: `https://login.microsoftonline.com/{tenant}/v2.0/.well-known/openid-configuration`
- Check firewall/proxy settings

**Error: "Token signature validation failed"**
- Ensure token is a valid Azure AD token (RS256 algorithm)
- Verify token is not expired (`exp` claim)
- Check token issuer (`iss` claim) matches Azure AD tenant

**Error: "JWKS cache expired"**
- Normal behavior: Keys are re-fetched after cache TTL expires (default 24 hours)
- Adjust `JwksCacheTtl` in appsettings if needed (in hours)

### Azure AD vs Local Mode Comparison

| Feature | Local Mode | Azure AD Mode |
|---------|-----------|---------------|
| Algorithm | HS256 (Symmetric) | RS256 (Asymmetric) |
| Key Source | JwtSecret (config) | JWKS endpoint (Azure AD) |
| Token Issuer | Self-signed | Azure AD |
| Key Rotation | Manual | Automatic (via JWKS) |
| Multi-tenant | Not supported | Supported (tenant isolation) |
| Production Ready | ❌ Development only | ✅ Production ready |
| Setup Complexity | Low (just JwtSecret) | Medium (Azure AD app registration) |
| Security | Lower (shared secret) | Higher (public/private keys) |

### Implementation Details

**Configuration Code (Program.cs):**

The test app automatically configures validation mode based on appsettings:

```csharp
// Read Mode from configuration
var mode = builder.Configuration["PrimusIdentity:Mode"] ?? "Local";
options.Mode = Enum.Parse<ValidationMode>(mode, ignoreCase: true);

// Local mode settings
if (options.Mode == ValidationMode.Local) {
    options.JwtSecret = builder.Configuration["PrimusIdentity:JwtSecret"] 
        ?? throw new InvalidOperationException("JwtSecret is required for Local mode");
}

// Azure AD mode settings
if (options.Mode == ValidationMode.AzureAd || options.Mode == ValidationMode.Hybrid) {
    options.TenantId = builder.Configuration["PrimusIdentity:TenantId"] 
        ?? throw new InvalidOperationException("TenantId is required for Azure AD mode");
    
    // Optional: JWKS cache TTL
    if (int.TryParse(builder.Configuration["PrimusIdentity:JwksCacheTtl"], out var cacheTtl)) {
        options.JwksCacheTtl = TimeSpan.FromHours(cacheTtl);
    }
}
```

**Validation Flow:**

1. **Incoming Request**: Client sends request with `Authorization: Bearer {token}` header
2. **Mode Detection**: SDK reads `Mode` from configuration (Local/AzureAd/Hybrid)
3. **Local Mode**: Validates token signature using JwtSecret (HS256)
4. **Azure AD Mode**:
   - Extracts `kid` (key ID) from token header
   - Checks JWKS cache for matching public key
   - If not cached or expired, fetches keys from Azure AD JWKS endpoint
   - Validates token signature using public key (RS256)
   - Caches public keys for `JwksCacheTtl` duration
5. **Claims Extraction**: Extracts user claims (userId, email, name, roles)
6. **Authorization**: Enforces role-based access control (RBAC)

**JWKS Caching:**

- **Cache Key**: Tenant ID + Key ID (`{tenantId}:{kid}`)
- **Cache Duration**: Configurable via `JwksCacheTtl` (default: 24 hours)
- **Cache Invalidation**: Automatic after TTL expires
- **Concurrent Requests**: Cache is thread-safe and shared across all requests
- **Performance**: Avoids repeated calls to Azure AD JWKS endpoint

---

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
