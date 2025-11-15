# Primus SaaS Platform – Portal & Auth Module

**Version**: 1.1  
**Document Type**: Functional & Technical Requirements  
**Scope**: MVP for Portal + Authentication Module (Identity Validator) + Versioning/Release Flow

---

## 1. Vision & Goals

Primus SaaS Platform is a developer-focused platform that provides reusable horizontal modules (starting with Authentication) as NuGet and NPM packages.

### Key Principles

- **Primus modules run inside the client's backend application**
- **No user data, PII, or tokens are ever stored or processed by Primus at runtime**
- **Primus SaaS Platform Portal is an internal control plane for:**
  - Creating Application entries for client apps
  - Generating `PrimusClientId` identifiers
  - Producing documentation bundles with integration instructions
  - Managing module catalog and versions
  - Tracking which applications use which module versions
  - Generating upgrade notifications

- Code is hosted on GitHub, packages are published to npm and NuGet
- **First horizontal module**: Authentication Module (Identity Validator) with two modes:
  - Local Username/Password (local DB, local JWT issuance & validation)
  - Azure AD / OpenID Connect (frontend obtains Microsoft tokens; backend validates them)

---

## 3. High-Level Architecture

### 3.1 Components

#### Primus SaaS Platform Portal

**Internal web application** for Platform Admins.

**Responsibilities**:

- Admin authentication (simple username/password for MVP)
- Module catalog management (modules, versions, release notes)
- Application registry (create/manage client application entries)
- Documentation bundle generation (per-application integration documentation)
- Version tracking and upgrade notifications
- **Does NOT participate in runtime authentication for client apps**

#### Module SDKs / Packages

**.NET Package**: `Primus.SaaS.IdentityValidator` (NuGet)  
**Node/TS Package**: `@primus-saas/identity-validator` (NPM)

**Responsibilities**:

- Run inside client backend (in-process, no external calls)
- Validate tokens:
  - Local JWT tokens (for username/password mode)
  - Azure AD tokens (for Azure AD mode)
- Provide helper APIs for local JWT generation
- Extract user claims for authorization logic

#### GitHub Repositories & CI/CD

**Example repos**:

- `primus-saas-portal` (portal code)
- `primus-saas-identity-validator-dotnet`
- `primus-saas-identity-validator-node`

**CI pipelines**:

- Build & test modules
- Publish NuGet packages for .NET
- Publish NPM packages for Node/TS
- Deploy Portal (internal use)

#### Client Applications (External)

Applications built by customers:

- .NET Web API
- Node.js / Express
- Node.js / NestJS
- Future: Python, Go, Java, etc. (based on demand)

**Responsibilities**:

- Install Primus SDK packages (NuGet/NPM)
- Configure authentication mode (Local, Azure AD, or Hybrid)
- Implement login UX on frontend
- Store all user data, passwords, and business logic in own infrastructure

---

## 2. Actors & Roles

For MVP, there is **only one platform role**.

### 2.1 Primus SaaS Platform Admin

**Internal user** (Primus team).

**Responsibilities**:

- Logs into the Portal
- Manages module catalog (modules, versions, release notes)
- Creates Application entries representing client applications
- Generates Documentation bundles for each application
- Shares Documentation with client developers (via email, PDF, Confluence)
- Tracks which applications use which module versions
- Generates and sends upgrade notifications

**Note**: Client developers do NOT log into the portal in v1. The Admin uses the portal internally and shares generated documentation with clients through external channels.

### 2.2 Client Developer (External)

**Does not have portal access** in v1.

**Workflow**:

- Receives Documentation from Primus Admin (via email/PDF)
- Installs Primus module packages (NuGet/NPM)
- Configures modules using provided config snippets
- Integrates modules using provided code snippets
- Stores all user/auth data in their own infrastructure

### 2.3 End User (of Client Application)

- Application's end user
- Logs into client application (using username/password or Azure AD)
- **Never interacts with Primus SaaS Platform directly**
- All authentication happens in the client's backend

---

## 4. End-to-End Flows

### 4.1 Admin Module Release Flow

**Objective**: Admin releases a new version of a module and makes it available to clients.

**Steps**:
1. Admin updates module code in corresponding GitHub repo
2. Admin bumps version (Semantic Versioning: MAJOR.MINOR.PATCH) and creates a Git tag (e.g. `v1.0.0`)
3. GitHub CI builds and publishes:
   - NuGet package (`Primus.Dev.IdentityValidator`) to NuGet.org
   - NPM package (`@primus-dev/identity-validator`) to npmjs.com
4. Admin logs into Primus DevSaaS Portal
5. Admin registers new module version in Module Catalog:
   - Module Name: IdentityValidator
   - Version: e.g. 1.0.0
   - Type: .NET, Node
   - Stability: Stable / Beta
   - Breaking change flag: true/false
   - Release notes
6. Portal stores this information and makes it selectable for client applications

### 4.2 Client Application Onboarding Flow

**Objective**: Client developer creates an application in the portal and gets everything needed to integrate.

**Steps**:
1. Client developer logs into Primus DevSaaS Portal
2. Client navigates to "My Applications" and clicks "Create New Application"
3. Portal prompts for:
   - Application Name
   - Stack: .NET API, Node API (Express), Node API (NestJS) (MVP can just support .NET + generic Node)
4. Portal creates an Application record and generates:
   - `PrimusDevClientId` (unique identifier for this client application)
5. Client selects desired modules for this application:
   - IdentityValidator (required for MVP)
   - Future: RBAC, Logging, etc.
6. For each selected module, client chooses a version from available versions in Module Catalog (e.g., 1.0.0)
7. Portal generates a **Documentation Page** for this application:
   - Displays:
     - `PrimusDevClientId`
     - Selected modules & versions
     - Install commands (NuGet / NPM)
     - Configuration snippets (appsettings.json / .env templates)
     - Code snippets for integrating middleware

### 4.3 Client Integration Flow (Backend)

**Objective**: A client integrates Primus DevSaaS module into their existing/new backend application.

#### 4.3.1 .NET Example

**Install NuGet package**:
```bash
dotnet add package Primus.Dev.IdentityValidator --version 1.0.0
```

**Configuration** (from Documentation):
```json
// appsettings.json
{
  "PrimusDev": {
    "ClientId": "PD-CLI-123456"
  },
  "Auth": {
    "Mode": "AzureAd", // or "Local" or "Hybrid"
    "AzureAd": {
      "TenantId": "xxxxx-tenant-guid",
      "ClientId": "xxxxx-aad-app-id",
      "Audience": "api://xxxxx-aad-app-id",
      "Authority": "https://login.microsoftonline.com/xxxxx-tenant-guid/v2.0"
    },
    "Local": {
      "SigningKey": "your-very-strong-signing-key",
      "Issuer": "your-app",
      "Audience": "your-app-users",
      "TokenLifetimeMinutes": 60
    }
  }
}
```

**Wire up middleware in Program.cs**:
```csharp
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddPrimusIdentityValidator(options =>
{
    options.PrimusDevClientId = config["PrimusDev:ClientId"];
    options.AuthMode = config["Auth:Mode"]; // "AzureAd", "Local", "Hybrid"

    options.AzureAd.TenantId = config["Auth:AzureAd:TenantId"];
    options.AzureAd.ClientId = config["Auth:AzureAd:ClientId"];
    options.AzureAd.Audience = config["Auth:AzureAd:Audience"];
    options.AzureAd.Authority = config["Auth:AzureAd:Authority"];

    options.Local.SigningKey = config["Auth:Local:SigningKey"];
    options.Local.Issuer = config["Auth:Local:Issuer"];
    options.Local.Audience = config["Auth:Local:Audience"];
    options.Local.TokenLifetimeMinutes =
        int.Parse(config["Auth:Local:TokenLifetimeMinutes"] ?? "60");
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/me", [Authorize] (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        Id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        Name = user.Identity?.Name
    });
});

app.Run();
```

**For local username/password login**, the client's own controller will:
- Accept username/password
- Use the IdentityValidator library to validate credentials and issue local JWT
- Return JWT / set cookie

The module will expose functions like: `AuthenticateLocalUser(username, password)` and `GenerateLocalJwt(user)`.

#### 4.3.2 Node/TS Example

**Install NPM package**:
```bash
npm install @primus-dev/identity-validator@1.0.0
```

**Config** (e.g. .env or config.ts):
```
PRIMUS_DEV_CLIENT_ID=PD-CLI-123456

AUTH_MODE=AzureAd
AZURE_TENANT_ID=xxxxx-tenant-guid
AZURE_CLIENT_ID=xxxxx-aad-app-id
AZURE_AUDIENCE=api://xxxxx-aad-app-id

LOCAL_SIGNING_KEY=your-very-strong-signing-key
LOCAL_ISSUER=your-app
LOCAL_AUDIENCE=your-app-users
LOCAL_TOKEN_LIFETIME_MINUTES=60
```

**Node/Express wiring**:
```typescript
import express from "express";
import { createIdentityValidator } from "@primus-dev/identity-validator";

const app = express();

const auth = createIdentityValidator({
  primusDevClientId: process.env.PRIMUS_DEV_CLIENT_ID!,
  mode: process.env.AUTH_MODE as "AzureAd" | "Local" | "Hybrid",
  azureAd: {
    tenantId: process.env.AZURE_TENANT_ID!,
    clientId: process.env.AZURE_CLIENT_ID!,
    audience: process.env.AZURE_AUDIENCE!,
  },
  local: {
    signingKey: process.env.LOCAL_SIGNING_KEY!,
    issuer: process.env.LOCAL_ISSUER!,
    audience: process.env.LOCAL_AUDIENCE!,
    tokenLifetimeMinutes: Number(process.env.LOCAL_TOKEN_LIFETIME_MINUTES || 60)
  }
});

app.get("/api/me", auth.requireAuth, (req, res) => {
  res.json({ user: req.user });
});

app.listen(5000, () => console.log("API running on 5000"));
```

### 4.4 Runtime Behavior

After integration:
- All authentication and token validation logic runs inside the client backend
- **For Azure AD mode**:
  - Frontend uses MSAL or similar to obtain Azure AD tokens
  - Tokens are sent to backend as `Authorization: Bearer <token>`
  - IdentityValidator validates token against Azure AD public keys & config
- **For Local mode**:
  - Client's login endpoint validates credentials via the library and issues local JWT
  - IdentityValidator validates local JWT for protected endpoints
- **Primus DevSaaS portal is not involved at runtime for authentication**

---

## 5. Versioning & Upgrade Flow

### 5.1 Versioning Rules

Use **Semantic Versioning**: `MAJOR.MINOR.PATCH`
- **PATCH** = bug fixes (no breaking changes)
- **MINOR** = backward-compatible feature additions
- **MAJOR** = breaking changes

Each module version is registered in the portal with:
- ModuleName
- Version
- IsBreakingChange (boolean)
- ReleaseNotes
- SupportedStacks (e.g., .NET, Node)

### 5.2 Portal Responsibilities

Track, per client Application:
- Which modules are attached
- Which version each module is using

Show **upgrade banners** when:
- Newer versions are available for any module

Provide **upgrade instructions**:
- Updated package versions (NuGet/NPM)
- Required config / code changes

Allow client developer to:
- Choose to upgrade to specific versions
- Optionally mark upgrade as completed

---

## 6. Portal Requirements (Functional)

### 6.1 Authentication (Portal itself)

Basic login/registration for:
- Primus Admins
- Client developers

For MVP, this can be simple username/password using any standard auth package.

### 6.2 Module Catalog

**Admin-only UI** to:
- Create modules (e.g., IdentityValidator, RBAC, Logging)
- Add versions for each module
- Mark versions as:
  - Stable
  - Beta
  - Deprecated
- Flag versions as `BreakingChange = true/false`
- Add `ReleaseNotes` text

### 6.3 Applications Management

**Client developer** can:
- Create new Application
  - Fields:
    - Name
    - Optional description
    - Stack (enum: .NET, Node)
- View Application details:
  - `PrimusDevClientId`
  - Modules and versions attached
  - Documentation bundle

### 6.4 Documentation Page

For each Application, portal must generate a **Documentation Page** containing:
- Application metadata
- `PrimusDevClientId`
- List of enabled modules
- For each module:
  - Package name
  - Selected version
  - Install commands (NuGet/NPM)
  - Config snippet templates for the selected stack
  - Code snippet showing minimal integration
- Optional: links to GitHub repos and full docs

### 6.5 Upgrade Management

Portal should show, in Application detail:
- Current module versions
- Available newer versions for each module
- Indication if newer versions are breaking

Provide per-module **"Upgrade Details"**:
- Target version
- Install commands with new version
- Code/config changes (text blocks)

---

## 7. Data Model (Portal)

A minimal relational model (can be implemented with any ORM):

### 7.1 Tables / Entities

#### Users
- `Id`
- `Email`
- `PasswordHash` (if local auth)
- `Role` (e.g., Admin / Client)
- Audit fields: `CreatedAt`, `UpdatedAt`

#### Modules
- `Id`
- `Name` (e.g., IdentityValidator)
- `Description`

#### ModuleVersions
- `Id`
- `ModuleId` (FK → Modules)
- `Version` (string, e.g. 1.0.0)
- `IsBreakingChange` (bool)
- `ReleaseNotes` (text)
- `SupportedStacks` (e.g., JSON array: ["dotnet", "node"])

#### Applications
- `Id`
- `OwnerUserId` (FK → Users)
- `Name`
- `Stack` (dotnet, node, etc.)
- `PrimusDevClientId` (string, unique)
- Audit fields: `CreatedAt`, `UpdatedAt`

#### ApplicationModules
- `Id`
- `ApplicationId` (FK → Applications)
- `ModuleId` (FK → Modules)
- `ModuleVersionId` (FK → ModuleVersions)
- Optional: `ConfigJson` (custom per-app config if needed later)

---

## 8. Non-Functional Requirements

### Security
- Portal must store only:
  - Client metadata
  - Module usage
- **Must not store any end-user identity information from client applications**

### Zero Runtime Dependency
- No client app should be required to call Primus DevSaaS APIs at runtime for authentication to work

### Extensibility
- Design portal and module data model to support additional modules later:
  - RBAC
  - Logging
  - Notifications
  - Observability

### Logging & Monitoring
- At minimum, log portal operations (admin changes, module version changes)

### CI/CD
- Basic pipelines for:
  - Portal deployment
  - Module packaging & publishing to npm and NuGet

---

## 9. Initial Folder Structure Suggestion (Monorepo)

```
primus-devsaas/
  portal/
    backend/              # ASP.NET Core 8 Web API
    frontend/             # React + TypeScript
    README.md

  modules/
    identity-validator-dotnet/
      src/
      tests/
      Primus.Dev.IdentityValidator.csproj
      README.md

    identity-validator-node/
      src/
      tests/
      package.json
      README.md

  examples/
    dotnet-api-example/
    node-api-example/

  docs/
    PRD.md  (this file)
    ARCHITECTURE.md

  .github/
    workflows/
      portal-ci.yml
      dotnet-module-ci.yml
      node-module-ci.yml
```

The portal can be implemented as:
- Backend: .NET 8 or Node/NestJS
- Frontend: React/Next or Angular

(Choice of exact stack is flexible as long as the behavior matches this spec.)

---

## 10. Success Criteria

This document is intended to be detailed enough for an AI assistant like VS Code Copilot or Cursor to:

1. Scaffold the portal with the described data model and pages
2. Scaffold .NET and Node/TS Identity Validator modules
3. Implement the flows described:
   - Module release integration (admin + catalog)
   - Client application onboarding
   - Documentation bundle generation
   - Version upgrade guidance

**No further clarification questions are required in this spec**; any reasonable defaults not explicitly described can be implemented in the most straightforward way that preserves:
- Zero runtime dependency on Primus DevSaaS for authentication
- No storage or processing of client end-user data by Primus DevSaaS
