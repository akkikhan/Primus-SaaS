# Module Integration & Configuration Flow

**Diagram Type**: Flowchart + Configuration Template  
**Purpose**: Shows how client applications integrate Primus SDK modules and configure authentication  
**Audience**: Client developers integrating IdentityValidator into their applications

---

## Integration Flowchart

```mermaid
flowchart TD
    Start([Client Developer<br/>Wants to Integrate]) --> Portal_Login{Login to<br/>DevSaaS Portal}
    
    Portal_Login -->|First Time| Register_App[Register New Application]
    Portal_Login -->|Existing App| Select_App[Select Existing Application]
    
    Register_App --> Fill_Details[Fill Application Details:<br/>- Name<br/>- Description<br/>- Stack (.NET/Node.js/Python)]
    Fill_Details --> Generate_ClientID[Portal Generates:<br/>- Primus ClientID (GUID)<br/>- Configuration Template]
    
    Generate_ClientID --> Select_Modules
    Select_App --> Select_Modules[Select Modules to Add]
    
    Select_Modules --> Choose_Versions[Choose Module Versions:<br/>- IdentityValidator v2.1.0<br/>- DataProtection v1.5.3<br/>- Etc.]
    
    Choose_Versions --> Review_Breaking{Breaking<br/>Changes?}
    
    Review_Breaking -->|Yes| Show_Migration[Portal Shows:<br/>- Breaking Changes List<br/>- Migration Steps<br/>- Code Examples]
    Review_Breaking -->|No| Generate_Docs
    
    Show_Migration --> Confirm_Accept{Accept<br/>Changes?}
    Confirm_Accept -->|No| Choose_Versions
    Confirm_Accept -->|Yes| Generate_Docs[Portal Auto-Generates:<br/>- Integration Documentation<br/>- Configuration Files<br/>- Code Snippets]
    
    Generate_Docs --> Download_Docs[Download/Copy:<br/>- README.md<br/>- appsettings.json<br/>- Package Install Commands]
    
    Download_Docs --> Install_Package[Install SDK Package]
    
    Install_Package --> Stack_Choice{Tech<br/>Stack?}
    
    Stack_Choice -->|.NET| Install_NuGet[Install NuGet Package:<br/>dotnet add package<br/>PrimusSaaS.Identity.Validator]
    Stack_Choice -->|Node.js| Install_NPM[Install NPM Package:<br/>npm install<br/>primus-identity-validator]
    Stack_Choice -->|Python| Install_PyPI[Install PyPI Package:<br/>pip install<br/>primus-identity-validator]
    
    Install_NuGet --> Configure_DotNet[Configure in Program.cs:<br/>- Add services<br/>- Add middleware]
    Install_NPM --> Configure_NodeJS[Configure in index.ts:<br/>- Import module<br/>- Add middleware]
    Install_PyPI --> Configure_Python[Configure in main.py:<br/>- Import module<br/>- Add middleware]
    
    Configure_DotNet --> Add_Config_File_DotNet[Add Configuration:<br/>appsettings.json]
    Configure_NodeJS --> Add_Config_File_Node[Add Configuration:<br/>.env or config.json]
    Configure_Python --> Add_Config_File_Python[Add Configuration:<br/>config.yaml or .env]
    
    Add_Config_File_DotNet --> Azure_Setup
    Add_Config_File_Node --> Azure_Setup
    Add_Config_File_Python --> Azure_Setup
    
    Azure_Setup{Azure AD<br/>Setup?}
    
    Azure_Setup -->|New Setup| Create_AAD_App[Create Azure AD App Registration:<br/>1. Azure Portal > App Registrations<br/>2. New Registration<br/>3. Configure API Permissions<br/>4. Create Client Secret]
    Azure_Setup -->|Existing| Use_Existing_AAD[Use Existing Azure AD App:<br/>- Copy Tenant ID<br/>- Copy Client ID]
    
    Create_AAD_App --> AAD_Config[Collect Azure AD Details:<br/>- Tenant ID<br/>- Client ID<br/>- Authority URL]
    Use_Existing_AAD --> AAD_Config
    
    AAD_Config --> Update_Config[Update Configuration File:<br/>- Set TenantId<br/>- Set ClientId<br/>- Set Audience<br/>- Set Authority]
    
    Update_Config --> Frontend_Integration[Integrate Frontend:<br/>- Install MSAL library<br/>- Configure MSAL<br/>- Add login/logout buttons]
    
    Frontend_Integration --> Test_Local[Test Locally:<br/>1. Run backend API<br/>2. Run frontend SPA<br/>3. Login with Azure AD<br/>4. Call protected API]
    
    Test_Local --> Test_Result{Tests<br/>Pass?}
    
    Test_Result -->|No| Debug[Debug Issues:<br/>- Check configuration<br/>- Verify Azure AD setup<br/>- Check logs<br/>- Review portal docs]
    Debug --> Test_Local
    
    Test_Result -->|Yes| Deploy[Deploy to Environment:<br/>- Update prod config<br/>- Deploy backend<br/>- Deploy frontend<br/>- Monitor logs]
    
    Deploy --> Monitor[Monitor & Maintain:<br/>- Check portal for updates<br/>- Review breaking changes<br/>- Update modules as needed]
    
    Monitor --> End([Integration Complete])
    
    style Start fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    style End fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    style Portal_Login fill:#fff9c4,stroke:#f57f17,stroke-width:2px
    style Review_Breaking fill:#ffccbc,stroke:#bf360c,stroke-width:2px
    style Azure_Setup fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    style Test_Result fill:#ffccbc,stroke:#bf360c,stroke-width:2px
    style Debug fill:#ffebee,stroke:#c62828,stroke-width:2px
```

---

## Configuration Templates

### 1. .NET Application (ASP.NET Core)

#### Step 1: Install Package
```bash
# Install via NuGet Package Manager Console
dotnet add package PrimusSaaS.Identity.Validator --version 2.1.0

# Or via Package Manager UI in Visual Studio
```

#### Step 2: Configure Services (Program.cs)
```csharp
using PrimusSaaS.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity Validator
builder.Services.AddPrimusIdentityValidator(options =>
{
    // Option 1: Load from configuration
    builder.Configuration.Bind("PrimusIdentityValidator", options);
    
    // Option 2: Configure in code (not recommended for production)
    // options.Mode = ValidationMode.AzureAd;
    // options.AzureAd = new AzureAdOptions
    // {
    //     TenantId = "your-tenant-id",
    //     ClientId = "your-client-id",
    //     Audience = "api://your-client-id"
    // };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add Primus Identity Validator Middleware
app.UsePrimusIdentityValidator();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### Step 3: Configuration File (appsettings.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  
  "PrimusIdentityValidator": {
    "Mode": "AzureAd",
    "PrimusClientId": "abc123-your-primus-client-id",
    
    "AzureAd": {
      "TenantId": "<YOUR_AZURE_TENANT_ID>",
      "ClientId": "<YOUR_AZURE_APP_CLIENT_ID>",
      "Audience": "api://<YOUR_AZURE_APP_CLIENT_ID>",
      "Authority": "https://login.microsoftonline.com/<YOUR_AZURE_TENANT_ID>/v2.0"
    },
    
    "JwksCache": {
      "Enabled": true,
      "ExpirationMinutes": 1440
    },
    
    "Logging": {
      "LogValidationSuccess": false,
      "LogValidationFailure": true,
      "IncludeTokenClaims": false
    },
    
    "Observability": {
      "EnableTelemetry": true,
      "InstrumentationKey": "<YOUR_APP_INSIGHTS_KEY>"
    }
  }
}
```

#### Step 4: Environment-Specific Configuration (appsettings.Development.json)
```json
{
  "PrimusIdentityValidator": {
    "Mode": "LocalJwt",
    "LocalJwt": {
      "Issuer": "https://localhost:5001",
      "Audience": "local-api",
      "SigningKey": "your-development-signing-key-min-32-chars"
    },
    "Logging": {
      "LogValidationSuccess": true,
      "IncludeTokenClaims": true
    }
  }
}
```

#### Step 5: Protect Controllers
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires valid JWT token
public class WeatherForecastController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Access authenticated user
        var userId = User.FindFirst("sub")?.Value;
        var userName = User.FindFirst("name")?.Value;
        var userEmail = User.FindFirst("email")?.Value;
        
        // Check roles (if using Azure AD app roles)
        if (User.IsInRole("Admin"))
        {
            // Admin-only logic
        }
        
        return Ok(new { userId, userName, userEmail });
    }
    
    [HttpGet("public")]
    [AllowAnonymous] // No authentication required
    public IActionResult GetPublic()
    {
        return Ok(new { message = "Public endpoint" });
    }
}
```

---

### 2. Node.js Application (Express)

#### Step 1: Install Package
```bash
# Install via npm
npm install primus-identity-validator

# Or via yarn
yarn add primus-identity-validator
```

#### Step 2: Configure Middleware (src/index.ts)
```typescript
import express from 'express';
import { primusIdentityValidator, ValidationMode } from 'primus-identity-validator';
import dotenv from 'dotenv';

dotenv.config();

const app = express();

// Add Primus Identity Validator Middleware
app.use(primusIdentityValidator({
  mode: ValidationMode.AzureAd,
  primusClientId: process.env.PRIMUS_CLIENT_ID!,
  
  azureAd: {
    tenantId: process.env.AZURE_TENANT_ID!,
    clientId: process.env.AZURE_CLIENT_ID!,
    audience: `api://${process.env.AZURE_CLIENT_ID}`,
    authority: `https://login.microsoftonline.com/${process.env.AZURE_TENANT_ID}/v2.0`
  },
  
  jwksCache: {
    enabled: true,
    expirationMinutes: 1440 // 24 hours
  },
  
  logging: {
    logValidationSuccess: false,
    logValidationFailure: true,
    includeTokenClaims: false
  },
  
  observability: {
    enableTelemetry: true,
    instrumentationKey: process.env.APP_INSIGHTS_KEY
  }
}));

// Protected route (requires authentication)
app.get('/api/weather', (req, res) => {
  // Access authenticated user from req.user (set by middleware)
  const user = req.user; // { sub, name, email, roles, ... }
  
  res.json({
    userId: user.sub,
    userName: user.name,
    userEmail: user.email
  });
});

// Public route (no authentication)
app.get('/api/public', (req, res) => {
  res.json({ message: 'Public endpoint' });
});

app.listen(3000, () => {
  console.log('Server running on http://localhost:3000');
});
```

#### Step 3: Configuration File (.env)
```bash
# Primus Configuration
PRIMUS_CLIENT_ID=abc123-your-primus-client-id

# Azure AD Configuration
AZURE_TENANT_ID=<YOUR_AZURE_TENANT_ID>
AZURE_CLIENT_ID=<YOUR_AZURE_APP_CLIENT_ID>

# Observability
APP_INSIGHTS_KEY=<YOUR_APP_INSIGHTS_KEY>

# Environment
NODE_ENV=production
```

#### Step 4: Development Configuration (.env.development)
```bash
# Primus Configuration
PRIMUS_CLIENT_ID=abc123-your-primus-client-id

# Local JWT Configuration (for testing without Azure AD)
PRIMUS_MODE=LocalJwt
LOCAL_JWT_ISSUER=http://localhost:3000
LOCAL_JWT_AUDIENCE=local-api
LOCAL_JWT_SIGNING_KEY=your-development-signing-key-min-32-chars

# Environment
NODE_ENV=development
```

#### Step 5: TypeScript Type Definitions
```typescript
// src/types/express.d.ts
import { User } from 'primus-identity-validator';

declare global {
  namespace Express {
    interface Request {
      user?: User;
    }
  }
}

export {};
```

---

### 3. Frontend Integration (React + MSAL)

#### Step 1: Install MSAL
```bash
npm install @azure/msal-browser @azure/msal-react
```

#### Step 2: Configure MSAL (src/authConfig.ts)
```typescript
import { Configuration, PopupRequest } from '@azure/msal-browser';

// MSAL Configuration
export const msalConfig: Configuration = {
  auth: {
    clientId: '<YOUR_AZURE_CLIENT_ID>', // Your Azure AD app registration client ID
    authority: 'https://login.microsoftonline.com/<YOUR_TENANT_ID>',
    redirectUri: 'http://localhost:3000/auth/callback', // Or your production URL
    postLogoutRedirectUri: 'http://localhost:3000'
  },
  cache: {
    cacheLocation: 'memory', // Use 'memory' for security (never 'localStorage')
    storeAuthStateInCookie: false
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        console.log(message);
      }
    }
  }
};

// Scopes for API access
export const loginRequest: PopupRequest = {
  scopes: ['api://<YOUR_API_CLIENT_ID>/access_as_user'] // Your backend API scope
};
```

#### Step 3: Setup MSAL Provider (src/main.tsx)
```typescript
import React from 'react';
import ReactDOM from 'react-dom/client';
import { MsalProvider } from '@azure/msal-react';
import { PublicClientApplication } from '@azure/msal-browser';
import App from './App';
import { msalConfig } from './authConfig';

// Create MSAL instance
const msalInstance = new PublicClientApplication(msalConfig);

// Initialize MSAL
await msalInstance.initialize();

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  </React.StrictMode>
);
```

#### Step 4: Login Component (src/components/Login.tsx)
```typescript
import React from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../authConfig';

export const Login: React.FC = () => {
  const { instance, accounts } = useMsal();
  
  const handleLogin = async () => {
    try {
      await instance.loginPopup(loginRequest);
    } catch (error) {
      console.error('Login failed:', error);
    }
  };
  
  const handleLogout = async () => {
    try {
      await instance.logoutPopup();
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };
  
  const isAuthenticated = accounts.length > 0;
  
  return (
    <div>
      {!isAuthenticated ? (
        <button onClick={handleLogin}>Sign In</button>
      ) : (
        <div>
          <p>Welcome, {accounts[0].name}</p>
          <button onClick={handleLogout}>Sign Out</button>
        </div>
      )}
    </div>
  );
};
```

#### Step 5: API Call with Token (src/services/api.ts)
```typescript
import axios from 'axios';
import { msalInstance } from '../main';
import { loginRequest } from '../authConfig';

const apiClient = axios.create({
  baseURL: 'https://your-api.azurewebsites.net',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Add token to all requests
apiClient.interceptors.request.use(
  async (config) => {
    const accounts = msalInstance.getAllAccounts();
    
    if (accounts.length > 0) {
      try {
        // Acquire token silently (MSAL handles caching and refresh)
        const response = await msalInstance.acquireTokenSilent({
          ...loginRequest,
          account: accounts[0]
        });
        
        // Add token to request
        config.headers.Authorization = `Bearer ${response.accessToken}`;
      } catch (error) {
        console.error('Token acquisition failed:', error);
        
        // Fallback: Try interactive login
        try {
          const response = await msalInstance.acquireTokenPopup(loginRequest);
          config.headers.Authorization = `Bearer ${response.accessToken}`;
        } catch (popupError) {
          console.error('Interactive token acquisition failed:', popupError);
          throw popupError;
        }
      }
    }
    
    return config;
  },
  (error) => Promise.reject(error)
);

// Example API call
export const getWeatherForecast = async () => {
  const response = await apiClient.get('/api/weather');
  return response.data;
};
```

---

## Azure AD App Registration Steps

### Step 1: Create App Registration
1. Navigate to **Azure Portal** → **Azure Active Directory** → **App Registrations**
2. Click **New registration**
3. Fill details:
   - **Name**: `MyApp API` (or your API name)
   - **Supported account types**: `Accounts in this organizational directory only`
   - **Redirect URI**: (Leave blank for API)
4. Click **Register**

### Step 2: Configure API Permissions
1. Go to **API permissions**
2. Click **Add a permission** → **Microsoft Graph**
3. Select **Delegated permissions**:
   - `User.Read` (for user profile)
4. Click **Add permissions**
5. Click **Grant admin consent** (if you're admin)

### Step 3: Expose API (For Backend)
1. Go to **Expose an API**
2. Click **Add a scope**
3. Set **Application ID URI**: `api://<YOUR_CLIENT_ID>` (default is fine)
4. Fill scope details:
   - **Scope name**: `access_as_user`
   - **Who can consent**: `Admins and users`
   - **Admin consent display name**: `Access API as user`
   - **Admin consent description**: `Allows the app to access the API on behalf of the user`
5. Click **Add scope**

### Step 4: Create Client Secret (For Backend Config)
1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Fill details:
   - **Description**: `Backend API Secret`
   - **Expires**: `24 months` (or your preference)
4. Click **Add**
5. **Copy the secret value immediately** (you won't see it again)

### Step 5: Configure SPA Authentication (For Frontend)
1. Create a **second app registration** for the SPA:
   - **Name**: `MyApp SPA`
   - **Redirect URI**: 
     - **Type**: `Single-page application (SPA)`
     - **URI**: `http://localhost:3000/auth/callback` (dev) or your production URL
2. Go to **API permissions** → **Add a permission** → **My APIs**
3. Select your backend API (`MyApp API`)
4. Select **Delegated permissions** → `access_as_user`
5. Click **Add permissions**

### Step 6: Collect Configuration Values
Copy these values for your configuration files:

| Value | Where to Find | Used In |
|---|---|---|
| **Tenant ID** | App Registration → Overview → Directory (tenant) ID | Backend & Frontend |
| **Client ID (API)** | Backend App Registration → Overview → Application (client) ID | Backend config |
| **Client ID (SPA)** | Frontend App Registration → Overview → Application (client) ID | Frontend config |
| **Client Secret** | Backend App Registration → Certificates & secrets | Backend config (secure storage) |

---

## Integration Checklist

### Backend Integration
- [ ] Install Primus SDK package (NuGet/NPM)
- [ ] Add SDK middleware to application startup
- [ ] Create configuration file (appsettings.json / .env)
- [ ] Set Azure AD Tenant ID, Client ID, Audience
- [ ] Configure JWKS caching (enable + set TTL)
- [ ] Add `[Authorize]` attribute to protected controllers
- [ ] Test with Postman (manual token)
- [ ] Configure observability (Application Insights)

### Frontend Integration
- [ ] Install MSAL library (@azure/msal-browser)
- [ ] Create MSAL configuration (authConfig.ts)
- [ ] Setup MSAL Provider in app root
- [ ] Implement login/logout UI components
- [ ] Create API client with token interceptor
- [ ] Test end-to-end flow (login → API call)
- [ ] Handle token refresh (MSAL automatic)
- [ ] Add error handling (401 → redirect to login)

### Azure AD Setup
- [ ] Create backend API app registration
- [ ] Expose API scope (`access_as_user`)
- [ ] Create frontend SPA app registration
- [ ] Configure redirect URIs (dev + prod)
- [ ] Grant API permissions (SPA → Backend)
- [ ] Create client secret (for backend only)
- [ ] Document configuration values (secure storage)

### DevSaaS Portal Setup
- [ ] Register application in portal
- [ ] Select IdentityValidator module + version
- [ ] Copy Primus ClientID (GUID)
- [ ] Download auto-generated integration docs
- [ ] Review breaking changes (if any)
- [ ] Bookmark application details page (for future updates)

### Testing
- [ ] Unit tests (mock token validation)
- [ ] Integration tests (real Azure AD tokens)
- [ ] E2E tests (SPA login → API call)
- [ ] Test token expiry + refresh
- [ ] Test invalid token handling
- [ ] Test missing token handling
- [ ] Test Azure AD key rotation (cache refresh)

### Deployment
- [ ] Update production configuration (secure storage)
- [ ] Deploy backend API (Azure App Service / Docker)
- [ ] Deploy frontend SPA (Azure Static Web Apps / CDN)
- [ ] Configure production Azure AD redirect URIs
- [ ] Enable monitoring (Application Insights)
- [ ] Set up alerts (auth failures, high latency)
- [ ] Document rollback procedure

---

## Troubleshooting Common Issues

### Issue 1: 401 Unauthorized (Invalid Signature)
**Cause**: JWT signature validation failed  
**Diagnosis**:
- Check `aud` claim in token (must match backend's `ClientId`)
- Check `iss` claim (must match Azure AD tenant)
- Verify token was issued for correct API (not SPA client ID)

**Fix**:
- Ensure SPA requests token with scope `api://<BACKEND_CLIENT_ID>/access_as_user`
- Verify backend `Audience` config matches SPA's requested scope

### Issue 2: 401 Unauthorized (Token Expired)
**Cause**: Token `exp` claim is in the past  
**Diagnosis**:
- Check token expiry: Decode JWT at [jwt.ms](https://jwt.ms)
- Typical expiry: 1 hour from issuance

**Fix**:
- Implement token refresh in frontend (MSAL automatic)
- Call `acquireTokenSilent()` before API calls

### Issue 3: CORS Errors (Frontend)
**Cause**: Backend not allowing frontend origin  
**Diagnosis**:
- Browser console shows CORS preflight failure
- Backend doesn't return `Access-Control-Allow-Origin` header

**Fix**:
```csharp
// Program.cs (.NET)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourapp.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors(); // Before app.UseAuthorization()
```

### Issue 4: JWKS Fetch Timeout
**Cause**: Network issue reaching Azure AD JWKS endpoint  
**Diagnosis**:
- Backend logs show `HttpClient` timeout
- Validation fails on cache miss

**Fix**:
- Increase JWKS cache TTL (reduce fetch frequency)
- Configure HTTP timeout in SDK options
- Check firewall/network rules (allow outbound HTTPS to login.microsoftonline.com)

### Issue 5: Wrong Tenant Configuration
**Cause**: `TenantId` in config doesn't match token issuer  
**Diagnosis**:
- Token `iss` claim: `https://login.microsoftonline.com/{tenant-X}/v2.0`
- Backend config: `TenantId = {tenant-Y}`

**Fix**:
- Verify Azure AD Tenant ID in portal (copy from App Registration → Overview)
- Ensure SPA uses same tenant in MSAL config

---

## Next Steps

After successful integration:

1. **Monitor Usage**: Check portal for API call statistics (Phase 2 feature)
2. **Update Modules**: Portal will notify of new versions or breaking changes
3. **Add More Modules**: Browse portal catalog for additional features (DataProtection, RateLimiting, etc.)
4. **Review Security**: Periodic security audits, token validation best practices
5. **Scale**: SDK performance scales horizontally with your infrastructure

---

**References**:
- **Portal**: [https://portal.primus.com](https://portal.primus.com)
- **Documentation**: `docs/ARCHITECTURE.md`, `docs/FAQ.md`
- **Azure AD Setup**: [Microsoft Identity Platform Docs](https://learn.microsoft.com/en-us/azure/active-directory/develop/)
- **MSAL.js Guide**: [MSAL React Tutorial](https://learn.microsoft.com/en-us/azure/active-directory/develop/tutorial-v2-react)
