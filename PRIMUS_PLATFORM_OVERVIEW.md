# 🚀 Primus SaaS Platform Overview

**Version**: 1.0  
**Focus**: IdentityValidator Module (First Building Block)  
**Last Updated**: November 22, 2025

---

## 📋 Table of Contents

1. [Platform Vision](#platform-vision)
2. [What Primus Portal Provides](#what-primus-portal-provides)
3. [IdentityValidator Module](#identityvalidator-module)
4. [How It Works](#how-it-works)
5. [Value Proposition](#value-proposition)
6. [Current Implementation Status](#current-implementation-status)

---

## 🎯 Platform Vision

Primus is a **horizontal, reusable SaaS framework** that provides versioned, managed modules to accelerate application development across multiple clients and identity providers.

**Core Principle**: 
> "Portal is for management and documentation. Runtime logic runs inside client applications. Portal is NOT in the execution path."

---

## 🏢 What Primus Portal Provides

### **1. Module Catalog**

**Current Focus: IdentityValidator**
- ✅ Version 1.0.0 (Current)
- 🔄 Version 2.0.0 (Future - with breaking changes)
- 📦 Distributed as:
  - NPM package: `primus-identity-validator`
  - NuGet package: `PrimusSaaS.Identity.Validator`

**Future Modules** (Not in scope yet):
- Audit Module
- Telemetry Module
- Feature Flags Module

---

### **2. Application Management**

#### **Track Which Apps Use Which Modules**

**Portal Database Schema**:
```
Applications
├─ Id: 1
├─ Name: "Acme Financial Dashboard"
├─ PrimusClientId: "PSP-CLI-711224"
├─ Stack: "NodeJS"
└─ ApplicationModules
   └─ ModuleId: 1 (IdentityValidator)
       ├─ Version: "1.0.0"
       ├─ IntegratedAt: "2025-11-15"
       └─ ConfigJson: "{}"
```

**What Portal Tracks**:
- ✅ Which applications exist
- ✅ Which modules each app uses
- ✅ Which version of each module
- ✅ When modules were integrated
- ✅ Module configuration (if any)

**What Portal Does NOT Track**:
- ❌ Azure AD credentials (developers configure in their apps)
- ❌ Runtime authentication tokens
- ❌ User login attempts
- ❌ API request logs (unless SDK reports them)

---

#### **PrimusClientId for Identification**

**Purpose**: Internal tracking identifier for portal management

**Format**: `PSP-CLI-XXXXXX` (e.g., `PSP-CLI-711224`)

**Used For**:
- ✅ Identifying applications in portal database
- ✅ Linking applications to modules
- ✅ Generating documentation
- ✅ Analytics and usage tracking (optional)
- ✅ Support and troubleshooting

**NOT Used For**:
- ❌ Token validation (Azure AD Client ID is used)
- ❌ Runtime authentication
- ❌ SDK configuration (except for optional analytics)

**Example**:
```javascript
// PrimusClientId is optional in SDK config
const primusAuth = primusIdentityValidator({
    // Required: Azure AD configuration
    defaultAuthority: "https://login.microsoftonline.com/common",
    allowedAudiences: ["api://your-azure-app-id"],
    
    // Optional: for analytics reporting to portal
    primusTrackingId: "PSP-CLI-711224"  // ✅ This is PrimusClientId
});
```

---

#### **Per-Environment Configuration**

**Portal Supports**:
- Development
- Testing
- Production

**What This Means**:
```
Application: "Acme Financial Dashboard"
├─ Development Environment
│  ├─ PrimusClientId: PSP-CLI-711224
│  ├─ Modules: IdentityValidator v1.0.0
│  └─ Config: { ... }
├─ Testing Environment
│  ├─ PrimusClientId: PSP-CLI-711224
│  ├─ Modules: IdentityValidator v1.0.0
│  └─ Config: { ... }
└─ Production Environment
   ├─ PrimusClientId: PSP-CLI-711224
   ├─ Modules: IdentityValidator v1.0.0
   └─ Config: { ... }
```

**Current Implementation**:
- ⚠️ Portal tracks applications but not per-environment config yet
- 🔄 Future enhancement: Separate configs per environment

---

### **3. Lifecycle Management**

#### **Breaking Change Detection**

**How It Works**:
```
1. Admin releases IdentityValidator v2.0.0
   ├─ Marks as "breaking change"
   └─ Provides migration guide

2. Portal scans database:
   ├─ Finds apps using v1.0.0
   └─ Flags them as "update available"

3. Portal shows in UI:
   ├─ "IdentityValidator v2.0.0 available"
   ├─ "Breaking changes detected"
   └─ "View migration guide"
```

**Database Schema**:
```csharp
public class ModuleVersion {
    public int Id { get; set; }
    public string Version { get; set; }  // "2.0.0"
    public bool IsBreakingChange { get; set; }  // true
    public string ReleaseNotes { get; set; }
    public string Changelog { get; set; }
    public string MigrationGuide { get; set; }  // New field needed
}
```

---

#### **Update Notifications**

**Portal Dashboard Shows**:
```
┌─────────────────────────────────────────────────────┐
│ Application: Acme Financial Dashboard               │
├─────────────────────────────────────────────────────┤
│ Modules:                                            │
│                                                     │
│ ✅ IdentityValidator v1.0.0                        │
│    ⚠️  Update Available: v2.0.0 (Breaking)         │
│    📋 View Migration Guide                          │
│    🔄 Update Now                                    │
└─────────────────────────────────────────────────────┘
```

**Notification Channels**:
- ✅ Portal UI (dashboard)
- 🔄 Email notifications (future)
- 🔄 Webhook notifications (future)

---

#### **Migration Guides**

**What Portal Provides**:
```markdown
# Migration Guide: IdentityValidator v1.0.0 → v2.0.0

## Breaking Changes

1. **Configuration Structure Changed**
   - Old: `clientId` and `clientSecret`
   - New: `defaultAuthority` and `allowedAudiences`

2. **Middleware Signature Changed**
   - Old: `primusIdentityMiddleware(config)`
   - New: `primusIdentityValidator(config)`

## Migration Steps

### Step 1: Update Package
```bash
npm install primus-identity-validator@2.0.0
```

### Step 2: Update Configuration
```javascript
// Old (v1.0.0)
const primusAuth = primusIdentityMiddleware({
    clientId: 'PSP-CLI-711224',
    clientSecret: 'psp_xxx...'
});

// New (v2.0.0)
const primusAuth = primusIdentityValidator({
    defaultAuthority: "https://login.microsoftonline.com/common",
    allowedAudiences: ["api://your-azure-app-id"]
});
```

### Step 3: Test
- Run unit tests
- Test authentication flow
- Verify token validation

### Step 4: Deploy
- Deploy to dev environment first
- Verify in testing
- Deploy to production
```

---

### **4. Documentation Generation**

#### **Auto-Generated Integration Guides**

**Portal Generates**:
```markdown
# Integration Guide: Acme Financial Dashboard

**Application ID**: PSP-CLI-711224  
**Stack**: Node.js  
**Module**: IdentityValidator v1.0.0

## Installation

```bash
npm install primus-identity-validator@1.0.0
```

## Configuration

```javascript
const { primusIdentityValidator } = require('primus-identity-validator');

const primusAuth = primusIdentityValidator({
    environment: "production",
    defaultAuthority: "https://login.microsoftonline.com/common",
    allowedAudiences: ["api://YOUR-AZURE-APP-ID"],
    
    // Optional: for analytics
    primusTrackingId: "PSP-CLI-711224"
});
```

## Usage

```javascript
app.get('/api/protected', primusAuth, (req, res) => {
    const user = req.primusUser;
    res.json({ user });
});
```
```

---

#### **Code Snippets**

**Portal Provides Stack-Specific Snippets**:

**Node.js / Express**:
```javascript
const express = require('express');
const { primusIdentityValidator } = require('primus-identity-validator');

const app = express();

const primusAuth = primusIdentityValidator({
    defaultAuthority: "https://login.microsoftonline.com/common",
    allowedAudiences: ["api://your-azure-app-id"]
});

app.get('/api/orders', primusAuth, (req, res) => {
    res.json({ orders: [], user: req.primusUser });
});

app.listen(3000);
```

**.NET / ASP.NET Core**:
```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusIdentityValidator(options =>
{
    options.DefaultAuthority = "https://login.microsoftonline.com/common";
    options.AllowedAudiences = new[] { "api://your-azure-app-id" };
});

var app = builder.Build();

app.UsePrimusIdentityValidator();
app.MapControllers();
app.Run();
```

---

#### **Configuration Templates**

**Portal Provides Environment-Specific Templates**:

**Development** (`.env.development`):
```bash
PRIMUS_TRACKING_ID=PSP-CLI-711224
AZURE_AD_AUTHORITY=https://login.microsoftonline.com/common
AZURE_AD_AUDIENCE=api://dev-your-azure-app-id
```

**Production** (`.env.production`):
```bash
PRIMUS_TRACKING_ID=PSP-CLI-711224
AZURE_AD_AUTHORITY=https://login.microsoftonline.com/common
AZURE_AD_AUDIENCE=api://prod-your-azure-app-id
```

---

## 🔐 IdentityValidator Module

### **What It Is**

A **reusable authentication middleware** that validates Azure AD (and other IdP) tokens in client applications.

**Key Characteristics**:
- ✅ Runs **inside client application** (not a separate service)
- ✅ Validates tokens **locally** (no portal call during auth)
- ✅ Supports **multiple identity providers** (Azure AD, Google, Local)
- ✅ Provides **multi-tenant resolution** (maps IdP tenant → Primus tenant)
- ✅ Versioned and managed through portal

---

### **What It Does**

```
┌─────────────────────────────────────────────────────────┐
│           Client Application Runtime                     │
│                                                          │
│  1. Request arrives with Authorization header           │
│     Authorization: Bearer <AzureADToken>                │
│                                                          │
│  2. IdentityValidator middleware executes               │
│     ├─ Extract token from header                        │
│     ├─ Fetch JWKS keys from Azure AD (cached)          │
│     ├─ Validate signature                               │
│     ├─ Validate issuer, audience, expiry               │
│     ├─ Resolve Primus tenant from token claims         │
│     ├─ Map Azure AD roles → Primus roles               │
│     └─ Attach user info to request                     │
│                                                          │
│  3. Request proceeds to controller                      │
│     ├─ req.primusUser available                         │
│     └─ req.primusTenantContext available                │
└─────────────────────────────────────────────────────────┘
```

---

### **What It Does NOT Do**

- ❌ Call Primus Portal for token validation
- ❌ Store or manage user credentials
- ❌ Handle login UI (that's MSAL/frontend)
- ❌ Issue tokens (Azure AD does that)
- ❌ Require portal to be running for auth to work

---

## 🔄 How It Works

### **Developer Onboarding Flow**

```
1. Developer logs into Primus Portal
   ↓
2. Creates application entry
   ├─ Name: "Acme Financial Dashboard"
   ├─ Stack: "Node.js"
   └─ Gets PrimusClientId: "PSP-CLI-711224"
   ↓
3. Selects modules
   └─ IdentityValidator v1.0.0
   ↓
4. Portal generates documentation
   ├─ Installation command
   ├─ Configuration snippet
   └─ Code examples
   ↓
5. Developer integrates into app
   ├─ npm install primus-identity-validator
   ├─ Copy configuration from portal
   └─ Add middleware to app
   ↓
6. App deployed
   └─ Authentication works WITHOUT portal in runtime path
```

---

### **Runtime Authentication Flow**

```
User → Azure AD Login → Azure AD Token
  ↓
Frontend holds token
  ↓
Frontend calls API: GET /api/orders
  Authorization: Bearer <AzureADToken>
  ↓
IdentityValidator middleware (in API)
  ├─ Fetch JWKS from Azure AD (cached)
  ├─ Validate token signature
  ├─ Validate issuer, audience, expiry
  ├─ Resolve Primus tenant
  ├─ Map roles
  └─ Attach user info to request
  ↓
API controller executes
  ├─ Access req.primusUser
  └─ Return protected data
  ↓
Response sent to frontend
```

**Key Point**: ⚠️ **Portal is NOT involved in this flow**

---

### **Optional Analytics Flow** (Non-blocking)

```
IdentityValidator (in client app)
  ↓
Periodically (e.g., every 5 minutes)
  ↓
POST /api/applications/PSP-CLI-711224/usage
  Body: {
    requestCount: 1000,
    errorCount: 5,
    avgLatencyMs: 45,
    timestamp: "2025-11-22T06:00:00Z"
  }
  ↓
Portal stores usage data
  ↓
Portal dashboard shows metrics
```

**This is optional and async** - authentication works without it.

---

## 💡 Value Proposition

### **Why Use Primus SDK Instead of Direct Azure AD Libraries?**

| Aspect | Direct Azure AD | Primus IdentityValidator |
|--------|----------------|--------------------------|
| **Setup Complexity** | High (learn OIDC, JWKS) | Low (one config object) |
| **Multi-IdP Support** | No (Azure only) | Yes (Azure, Google, Local) |
| **Multi-Tenant** | Manual mapping | Built-in resolution |
| **Role Mapping** | Manual | Configured in portal |
| **Version Management** | Manual | Portal tracks versions |
| **Update Notifications** | None | Portal notifies |
| **Documentation** | Generic | Auto-generated per app |
| **Code Reuse** | Repeat per app | Same code across apps |

---

### **Specific Benefits**

#### **1. Abstraction & Simplification**
```javascript
// Without Primus (using passport-azure-ad)
const passport = require('passport');
const BearerStrategy = require('passport-azure-ad').BearerStrategy;

const options = {
    identityMetadata: `https://login.microsoftonline.com/${tenantId}/v2.0/.well-known/openid-configuration`,
    clientID: clientId,
    validateIssuer: true,
    issuer: `https://login.microsoftonline.com/${tenantId}/v2.0`,
    audience: audience,
    loggingLevel: 'info',
    passReqToCallback: false
};

passport.use(new BearerStrategy(options, (token, done) => {
    // Manual tenant resolution
    // Manual role mapping
    // Manual error handling
    done(null, token);
}));

// With Primus
const primusAuth = primusIdentityValidator({
    defaultAuthority: "https://login.microsoftonline.com/common",
    allowedAudiences: ["api://your-app-id"]
});
// Done! Tenant resolution and role mapping built-in
```

#### **2. Multi-Tenant Resolution**
```javascript
// SDK automatically maps:
Azure AD Tenant (cbd15a9b-cd52-4ccc-916a-00e2edb13043)
  ↓
Primus Tenant ("acme-financial")
  ↓
Tenant-specific roles and policies
```

#### **3. Centralized Management**
- ✅ Portal tracks which apps use which versions
- ✅ Portal notifies about updates
- ✅ Portal provides migration guides
- ✅ One place to see all applications

---

## 📊 Current Implementation Status

### ✅ **What's Working**

1. **Portal Application Management**
   - ✅ Create applications
   - ✅ Assign PrimusClientId
   - ✅ Track applications in database

2. **Module Management**
   - ✅ IdentityValidator module exists
   - ✅ Version tracking (v1.0.0)
   - ✅ Module assignment to applications

3. **SDK Implementation**
   - ✅ Node.js SDK (`primus-identity-validator`)
   - ✅ .NET SDK (`PrimusSaaS.Identity.Validator`)
   - ✅ Azure AD token validation
   - ✅ Local mode support

4. **Documentation Generation**
   - ✅ Portal generates integration guides
   - ✅ Code snippets per stack
   - ✅ PDF export

---

### 🔄 **What Needs Improvement**

1. **Documentation Clarity**
   - ⚠️ Current docs show `clientId: 'PSP-CLI-711224'` which is confusing
   - ✅ Should clarify this is optional tracking ID
   - ✅ Should show Azure AD config as primary

2. **Per-Environment Configuration**
   - ⚠️ Portal doesn't track dev/test/prod configs separately
   - ✅ Should add environment support

3. **Breaking Change Management**
   - ⚠️ No migration guide field in database
   - ✅ Should add `MigrationGuide` to `ModuleVersion`

4. **Analytics Reporting**
   - ⚠️ SDK doesn't report usage to portal
   - ✅ Should add optional analytics endpoint

---

### ❌ **What's NOT Needed** (Based on ChatGPT Diagrams)

1. **Azure AD Credentials in Portal**
   - ❌ Portal doesn't need to store Azure AD Tenant ID
   - ❌ Portal doesn't need to store Azure AD Client ID
   - ✅ Developers configure these in their apps

2. **Portal in Runtime Auth Path**
   - ❌ SDK should NOT call portal for token validation
   - ✅ SDK validates tokens locally

3. **Two-Token Flow**
   - ❌ No need for Azure Token → Portal → Primus JWT
   - ✅ Direct Azure AD token validation

---

## 🎯 Summary

**Primus Portal is a Developer Console** that:
- ✅ Manages module catalog (IdentityValidator)
- ✅ Tracks which apps use which modules
- ✅ Generates documentation and code snippets
- ✅ Notifies about updates and breaking changes
- ❌ Is NOT in the runtime authentication path

**IdentityValidator is a Helper Library** that:
- ✅ Validates tokens locally in client apps
- ✅ Provides abstraction over OIDC/JWKS
- ✅ Supports multi-tenant resolution
- ✅ Maps roles automatically
- ❌ Does NOT call portal for validation

**PrimusClientId is a Tracking ID** that:
- ✅ Identifies applications in portal
- ✅ Links apps to modules
- ✅ Enables analytics (optional)
- ❌ Is NOT used for authentication

---

**Next Steps**: Focus on improving documentation clarity and adding per-environment configuration support.
