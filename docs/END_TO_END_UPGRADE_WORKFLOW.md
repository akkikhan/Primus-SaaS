# End-to-End Module Upgrade Workflow

**Document Version**: 1.0  
**Last Updated**: November 19, 2025  
**Purpose**: Complete guide for how clients discover, verify, and upgrade module versions in the Primus SaaS Platform

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture Components](#architecture-components)
3. [Complete Workflow](#complete-workflow)
4. [Step-by-Step Process](#step-by-step-process)
5. [Verification Methods](#verification-methods)
6. [npm Package Updates](#npm-package-updates)
7. [Breaking Changes Handling](#breaking-changes-handling)
8. [Automation Opportunities](#automation-opportunities)

---

## Overview

The Primus SaaS Platform provides a **centralized upgrade management system** that allows clients to:
- ✅ Discover when new versions of integrated modules are available
- ✅ View detailed changelogs and release notes
- ✅ Identify breaking changes before upgrading
- ✅ Upgrade modules with one-click from the portal
- ✅ Verify upgrades in their application code
- ✅ Update npm/NuGet packages to match portal versions

---

## Architecture Components

### 1. Portal Backend (Version Registry)
- **Database**: Stores all module versions with metadata
  - `Modules` table: Module definitions
  - `ModuleVersions` table: All published versions (1.0.0, 1.1.0, 1.2.0, etc.)
  - `ApplicationModules` table: Tracks which version each application uses
- **API Endpoints**:
  - `GET /api/upgrade/overview` - Lists all applications with available upgrades
  - `POST /api/upgrade/applications/{appId}/modules/{moduleId}/upgrade` - Performs upgrade
  - `GET /api/applications/{id}` - Shows current module versions per app

### 2. Portal Frontend (UI)
- **Application Details Page**: Shows current vs latest version per module
- **Upgrade Manager Page**: Centralized view of all upgradable modules across applications
- **Module Version Indicators**: Visual badges showing "Update Available" status

### 3. SDK Packages (npm/NuGet)
- **npm**: `primus-identity-validator@1.0.0` (published November 19, 2025)
- **NuGet**: `PrimusSaaS.Identity.Validator` (ready for publication)
- **Version Alignment**: Package versions must align with portal module versions

---

## Complete Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│                    MODULE LIFECYCLE WORKFLOW                     │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐
│   Step 1:    │  Platform Admin Publishes New Module Version
│   PUBLISH    │  ─────────────────────────────────────────────
└──────────────┘  • Admin creates new version in Modules Page
                  • Version: 1.1.0
                  • Release notes, changelog, breaking change flag
                  • Module stored in database (ModuleVersions table)
                        ↓
┌──────────────┐
│   Step 2:    │  System Detects Version Discrepancy
│   DETECT     │  ─────────────────────────────────────────────
└──────────────┘  • Portal queries ApplicationModules vs latest ModuleVersions
                  • SQL: SELECT WHERE current_version < latest_version
                  • Status flagged as "UpdateAvailable"
                        ↓
┌──────────────┐
│   Step 3:    │  Client Discovers Available Update
│   DISCOVER   │  ─────────────────────────────────────────────
└──────────────┘  • Client logs into portal
                  • Method A: Application Details Page
                    - Shows badge next to module: "v1.0.0 → v1.1.0 available"
                    - Orange pill: "Update Available"
                  • Method B: Upgrade Manager Page
                    - Centralized list: "X upgrades available"
                    - Table shows: Module | Current | Latest | Status
                        ↓
┌──────────────┐
│   Step 4:    │  Client Reviews Changes
│   REVIEW     │  ─────────────────────────────────────────────
└──────────────┘  • Client clicks "View Changelog"
                  • Modal displays:
                    - Release notes (feature summary)
                    - Detailed changelog (technical changes)
                    - Breaking change indicator (⚠️ red flag if true)
                  • Client assesses impact on their application
                        ↓
┌──────────────┐
│   Step 5:    │  Client Upgrades in Portal
│   UPGRADE    │  ─────────────────────────────────────────────
│  (PORTAL)    │  • Client clicks "Upgrade" button
└──────────────┘  • POST /api/upgrade/applications/{appId}/modules/{moduleId}/upgrade
                  • Backend updates ApplicationModules.ModuleVersionId
                  • Database now shows: App uses v1.1.0
                  • Portal UI refreshes: "UpToDate" status
                        ↓
┌──────────────┐
│   Step 6:    │  Client Verifies Portal Upgrade
│   VERIFY     │  ─────────────────────────────────────────────
│  (PORTAL)    │  • Application Details Page now shows:
└──────────────┘    - Module version: v1.1.0
                    - Status badge: Green "UpToDate"
                    - No "Update Available" pill
                  • Upgrade Manager shows 0 upgrades for this module
                        ↓
┌──────────────┐
│   Step 7:    │  Client Updates npm/NuGet Package
│   UPDATE     │  ─────────────────────────────────────────────
│  (CODE)      │  • Client opens their application codebase
└──────────────┘  • For Node.js:
                    - Check current version: npm list primus-identity-validator
                    - Update package.json: "primus-identity-validator": "^1.1.0"
                    - Run: npm install
                  • For .NET:
                    - Check: dotnet list package
                    - Update: dotnet add package PrimusSaaS.Identity.Validator -v 1.1.0
                        ↓
┌──────────────┐
│   Step 8:    │  Client Verifies Code Update
│   VERIFY     │  ─────────────────────────────────────────────
│  (CODE)      │  • Check installed version:
└──────────────┘    - Node.js: npm list primus-identity-validator
                      Output: primus-identity-validator@1.1.0
                    - .NET: dotnet list package | grep Primus
                      Output: PrimusSaaS.Identity.Validator 1.1.0
                  • Review package-lock.json or .csproj file
                  • Build application: npm run build / dotnet build
                  • Run tests: npm test / dotnet test
                        ↓
┌──────────────┐
│   Step 9:    │  Client Tests Integration
│   TEST       │  ─────────────────────────────────────────────
└──────────────┘  • Start application locally
                  • Test authentication endpoints
                  • Verify JWT validation works
                  • Check for breaking changes (if any)
                  • Run automated integration tests
                        ↓
┌──────────────┐
│  Step 10:    │  Client Deploys to Production
│   DEPLOY     │  ─────────────────────────────────────────────
└──────────────┘  • Commit package.json/csproj changes
                  • Push to repository
                  • CI/CD pipeline installs updated package
                  • Deploy to staging/production
                  • Monitor for errors
                        ↓
                  ✅ UPGRADE COMPLETE
```

---

## Step-by-Step Process

### Phase 1: Discovery (Portal)

#### Location 1: Application Details Page

1. **Navigate**: Click on an application from Applications Page
2. **View Modules Section**: Scroll to "Integrated Modules"
3. **Check Version Status**:
   ```
   ┌─────────────────────────────────────────────┐
   │  Module: Identity Validator                 │
   │  Current Version: v1.0.0                    │
   │  Latest Version: v1.1.0                     │
   │  Status: [🟠 Update Available]              │
   │  Actions: [Change Version] [View Changelog] │
   └─────────────────────────────────────────────┘
   ```

#### Location 2: Upgrade Manager Page

1. **Navigate**: Click "Upgrade Manager" in sidebar
2. **View Dashboard**: See all applications with available upgrades
3. **Check Totals**: Header shows "5 upgrades available"
4. **Review Table**:
   ```
   Application: My API
   ┌──────────────────┬─────────┬────────┬─────────────────┬──────────┐
   │ Module           │ Current │ Latest │ Status          │ Actions  │
   ├──────────────────┼─────────┼────────┼─────────────────┼──────────┤
   │ Identity Validator│ v1.0.0  │ v1.1.0 │ Update Available│ [Upgrade]│
   │ Analytics Module  │ v2.3.0  │ v2.3.0 │ Up to Date      │    -     │
   └──────────────────┴─────────┴────────┴─────────────────┴──────────┘
   ```

### Phase 2: Review Changes

1. **Click "View Changelog"** button
2. **Modal Opens** with detailed information:
   ```
   ┌──────────────────────────────────────────┐
   │  Identity Validator v1.0.0 → v1.1.0     │
   ├──────────────────────────────────────────┤
   │  ⚠️  BREAKING CHANGE                     │
   │                                          │
   │  Release Notes:                          │
   │  • Added support for Azure AD B2C       │
   │  • Improved JWKS caching performance     │
   │  • Fixed bug in role validation          │
   │                                          │
   │  Changelog:                              │
   │  • BREAKING: Changed requireRoles()     │
   │    function signature                    │
   │  • Added cacheJwks option (default: 24h)│
   │  • Updated jsonwebtoken to v9.0.2       │
   │                                          │
   │  Migration Guide:                        │
   │  Old: requireRoles('admin')             │
   │  New: requireRoles(['admin'])           │
   │                                          │
   │  [Close]                                 │
   └──────────────────────────────────────────┘
   ```

### Phase 3: Upgrade in Portal

1. **Click "Upgrade" button** (Application Details or Upgrade Manager)
2. **Confirm action** (if breaking change, extra warning shown)
3. **API Call**: `POST /api/upgrade/applications/5/modules/2/upgrade`
4. **Backend Process**:
   ```sql
   UPDATE ApplicationModules
   SET ModuleVersionId = (SELECT Id FROM ModuleVersions 
                          WHERE ModuleId = 2 
                          ORDER BY ReleasedAt DESC LIMIT 1)
   WHERE ApplicationId = 5 AND ModuleId = 2;
   ```
5. **Success Toast**: "Upgraded Identity Validator to v1.1.0"
6. **UI Refresh**: Status changes to "Up to Date"

### Phase 4: Verify Portal Upgrade

**Application Details Page Check**:
```
✅ Identity Validator
   Version: v1.1.0
   Status: [🟢 Up to Date]
   Last Updated: Nov 19, 2025 3:42 PM
```

**Upgrade Manager Check**:
```
My API: 0 upgrades available ✅
All modules are up to date
```

### Phase 5: Update Code Packages

#### For Node.js Applications

**Step 1: Check Current Version**
```bash
cd /path/to/your/application
npm list primus-identity-validator
```

**Output**:
```
my-app@1.0.0
└── primus-identity-validator@1.0.0
```

**Step 2: Update package.json**
```json
{
  "dependencies": {
    "primus-identity-validator": "^1.1.0"  // Changed from ^1.0.0
  }
}
```

**Step 3: Install Updated Package**
```bash
npm install
```

**Output**:
```
added 1 package, changed 1 package, audited 129 packages in 3s

primus-identity-validator@1.1.0
```

**Step 4: Verify Installation**
```bash
npm list primus-identity-validator
```

**Output**:
```
my-app@1.0.0
└── primus-identity-validator@1.1.0  ✅
```

#### For .NET Applications

**Step 1: Check Current Version**
```bash
cd /path/to/your/application
dotnet list package
```

**Output**:
```
Project 'MyApp' has the following package references
   [net7.0]:
   Top-level Package                        Requested   Resolved
   > PrimusSaaS.Identity.Validator          1.0.0       1.0.0
```

**Step 2: Update Package**
```bash
dotnet add package PrimusSaaS.Identity.Validator -v 1.1.0
```

**Output**:
```
info : Adding PackageReference for package 'PrimusSaaS.Identity.Validator' 
      into project 'MyApp.csproj'.
info : Package 'PrimusSaaS.Identity.Validator' is compatible with all frameworks
info : PackageReference for package 'PrimusSaaS.Identity.Validator' version '1.1.0' 
      added to file 'MyApp.csproj'.
```

**Step 3: Verify Installation**
```bash
dotnet list package
```

**Output**:
```
Project 'MyApp' has the following package references
   [net7.0]:
   Top-level Package                        Requested   Resolved
   > PrimusSaaS.Identity.Validator          1.1.0       1.1.0  ✅
```

### Phase 6: Code Verification

#### Verify npm Package Version

**Method 1: Check package-lock.json**
```json
{
  "packages": {
    "node_modules/primus-identity-validator": {
      "version": "1.1.0",  ✅
      "resolved": "https://registry.npmjs.org/primus-identity-validator/-/primus-identity-validator-1.1.0.tgz",
      "integrity": "sha512-...",
      "license": "MIT"
    }
  }
}
```

**Method 2: Check node_modules**
```bash
cat node_modules/primus-identity-validator/package.json | grep version
```

**Output**:
```json
"version": "1.1.0",  ✅
```

**Method 3: Runtime Check (Code)**
```typescript
import { version } from 'primus-identity-validator/package.json';
console.log(`Using primus-identity-validator v${version}`);
```

**Output**:
```
Using primus-identity-validator v1.1.0  ✅
```

#### Verify .NET Package Version

**Method 1: Check .csproj file**
```xml
<ItemGroup>
  <PackageReference Include="PrimusSaaS.Identity.Validator" Version="1.1.0" />  ✅
</ItemGroup>
```

**Method 2: Check obj/project.assets.json**
```json
{
  "targets": {
    ".NETCoreApp,Version=v7.0": {
      "PrimusSaaS.Identity.Validator/1.1.0": {  ✅
        "type": "package",
        "compile": { "lib/net7.0/PrimusSaaS.Identity.Validator.dll": {} }
      }
    }
  }
}
```

**Method 3: Runtime Check (Code)**
```csharp
using System.Reflection;

var assembly = Assembly.GetAssembly(typeof(PrimusIdentityMiddleware));
var version = assembly.GetName().Version;
Console.WriteLine($"Using PrimusSaaS.Identity.Validator v{version}");
```

**Output**:
```
Using PrimusSaaS.Identity.Validator v1.1.0  ✅
```

### Phase 7: Integration Testing

#### Test Checklist

```bash
# 1. Build Application
npm run build  # or dotnet build
✅ Build succeeded

# 2. Run Unit Tests
npm test  # or dotnet test
✅ All tests passing

# 3. Start Application
npm start  # or dotnet run
✅ Application started on port 3000

# 4. Test Authentication Endpoint
curl -X GET http://localhost:3000/api/protected \
  -H "Authorization: Bearer <JWT_TOKEN>"
✅ 200 OK - Token validated successfully

# 5. Test Role-Based Access
curl -X GET http://localhost:3000/api/admin \
  -H "Authorization: Bearer <ADMIN_JWT_TOKEN>"
✅ 200 OK - Admin role verified

# 6. Test Invalid Token
curl -X GET http://localhost:3000/api/protected \
  -H "Authorization: Bearer invalid-token"
✅ 401 Unauthorized - Invalid token rejected

# 7. Check Breaking Changes (if applicable)
# Review code for deprecated function calls
# Update as per migration guide
✅ Breaking changes addressed
```

---

## Verification Methods

### 1. Portal Verification (Version Alignment)

**What it verifies**: Portal database record of which module version the application is supposed to use

**How to check**:
1. Navigate to Application Details page
2. Check "Integrated Modules" section
3. Confirm version shows v1.1.0
4. Confirm status badge is "Up to Date" (green)

**API Verification**:
```bash
curl -X GET http://localhost:5000/api/applications/5 \
  -H "Authorization: Bearer <ADMIN_TOKEN>"
```

**Response**:
```json
{
  "id": 5,
  "name": "My API",
  "integratedModules": [
    {
      "moduleId": 2,
      "moduleName": "Identity Validator",
      "version": "1.1.0",  ✅
      "latestVersion": "1.1.0",
      "versionStatus": "UpToDate",  ✅
      "releasedAt": "2025-11-19T15:30:00Z"
    }
  ]
}
```

### 2. npm Registry Verification (Package Availability)

**What it verifies**: The npm package version exists on the public registry

**How to check**:
```bash
npm view primus-identity-validator versions
```

**Output**:
```
[ '1.0.0', '1.1.0' ]  ✅ Both versions published
```

**Check specific version metadata**:
```bash
npm view primus-identity-validator@1.1.0
```

**Output**:
```
primus-identity-validator@1.1.0 | MIT | deps: 2 | versions: 2
Official Node.js SDK for validating JWT tokens issued by Primus SaaS Portal

dist
.tarball: https://registry.npmjs.org/primus-identity-validator/-/primus-identity-validator-1.1.0.tgz
.integrity: sha512-...
.unpackedSize: 82.5 kB

dependencies:
axios: ^1.6.0
jsonwebtoken: ^9.0.2

published a day ago by akkhan001 <khanakkijpr@gmail.com>
```

### 3. Local Installation Verification (Your Code)

**What it verifies**: Your application actually has the updated package installed

**Node.js - Method 1: npm list**
```bash
npm list primus-identity-validator --depth=0
```

**Output**:
```
my-app@1.0.0
└── primus-identity-validator@1.1.0  ✅
```

**Node.js - Method 2: Check require.cache**
```javascript
const packagePath = require.resolve('primus-identity-validator/package.json');
const packageJson = require(packagePath);
console.log(`Loaded version: ${packageJson.version}`);
```

**Output**:
```
Loaded version: 1.1.0  ✅
```

**.NET - Method 1: dotnet list package**
```bash
dotnet list package --include-transitive | grep Primus
```

**Output**:
```
> PrimusSaaS.Identity.Validator    1.1.0    1.1.0  ✅
```

**.NET - Method 2: Assembly reflection**
```csharp
var version = typeof(PrimusIdentityMiddleware).Assembly.GetName().Version;
Console.WriteLine($"Loaded version: {version}");
```

**Output**:
```
Loaded version: 1.1.0  ✅
```

### 4. Runtime Verification (Application Behavior)

**What it verifies**: The new version is actually running in your application

**Method 1: Health Check Endpoint**
```bash
curl http://localhost:3000/health
```

**Response**:
```json
{
  "status": "healthy",
  "dependencies": {
    "primus-identity-validator": "1.1.0"  ✅
  }
}
```

**Method 2: Startup Logs**
```
[INFO] Application starting...
[INFO] Loading Primus Identity Validator v1.1.0  ✅
[INFO] JWKS cache enabled (TTL: 24h)  ✅ (new in v1.1.0)
[INFO] Azure AD B2C support enabled  ✅ (new in v1.1.0)
[INFO] Server listening on port 3000
```

**Method 3: Test Breaking Changes**

If v1.1.0 introduced breaking changes (e.g., function signature change):

**Old code (v1.0.0) - should fail**:
```typescript
app.get('/admin', requireRoles('admin'), (req, res) => {  // ❌ TypeError
  res.json({ message: 'Admin area' });
});
```

**Error**:
```
TypeError: requireRoles(...) is not a function
Expected: requireRoles(['admin'])  // v1.1.0 requires array
```

**Updated code (v1.1.0) - should work**:
```typescript
app.get('/admin', requireRoles(['admin']), (req, res) => {  // ✅
  res.json({ message: 'Admin area' });
});
```

---

## npm Package Updates

### Understanding Version Alignment

```
┌─────────────────────────────────────────────────────────┐
│           VERSION ALIGNMENT ARCHITECTURE                 │
└─────────────────────────────────────────────────────────┘

┌──────────────────────┐
│   npm Registry       │  Public package repository
│   registry.npmjs.org │  • primus-identity-validator@1.0.0  ✅
├──────────────────────┤  • primus-identity-validator@1.1.0  ✅
│  Version: 1.1.0      │  • Published by maintainer
│  Published: ✅       │  • Accessible via npm install
└──────────────────────┘
          ↑
          │ 1. Admin publishes to npm
          │
┌──────────────────────┐
│   Portal Database    │  Module version registry
│   ModuleVersions     │  • Stores all module versions
├──────────────────────┤  • Tracks release notes, changelogs
│  ModuleId: 2         │  • Links applications to versions
│  Version: 1.1.0      │
│  Status: Published   │
└──────────────────────┘
          ↑
          │ 2. Portal references version
          │
┌──────────────────────┐
│  ApplicationModules  │  Per-app version tracking
│  Table               │  • Each app has its own version
├──────────────────────┤  • Can be different across apps
│  AppId: 5            │  • Updated via upgrade API
│  ModuleId: 2         │
│  VersionId: 7        │ → Points to version 1.1.0
└──────────────────────┘
          ↑
          │ 3. Client upgrades in portal
          │
┌──────────────────────┐
│  Client Application  │  Your codebase
│  package.json        │  • Must manually update
├──────────────────────┤  • npm install required
│  dependencies: {     │  • Build and deploy
│    "primus-..": "^1.1.0"
│  }                   │
└──────────────────────┘
          ↑
          │ 4. Developer updates code
          │
┌──────────────────────┐
│  node_modules/       │  Installed packages
│  primus-identity-    │  • Actual running code
│  validator/          │  • Version must match portal
├──────────────────────┤
│  package.json        │
│  "version": "1.1.0"  │ ✅ VERIFIED
└──────────────────────┘
```

### Publishing Workflow (Admin Perspective)

**Step 1: Publish to npm**
```bash
cd sdk/nodejs/primus-identity-validator
npm version 1.1.0  # Updates package.json
npm run build      # Compile TypeScript
npm test           # Run all tests
npm publish        # Publish to npm registry
```

**Step 2: Create Module Version in Portal**
```
Navigate to: Modules Page
Click: "Identity Validator"
Click: "+ Add Version" button

Form:
  Version: 1.1.0
  Release Notes: "Added Azure AD B2C support, improved caching"
  Changelog: "- BREAKING: requireRoles now accepts array\n- Added cacheJwks option"
  Is Breaking Change: ☑ Yes

Click: "Publish Version"
```

**Result**: Portal now knows version 1.1.0 exists and can notify clients

### Update Discovery Timeline

```
Timeline: When does the client know an update exists?

T+0m   [Admin] Publishes v1.1.0 to npm registry
T+1m   [Admin] Creates v1.1.0 record in portal Modules page
T+2m   [System] Portal queries: SELECT * FROM ModuleVersions WHERE ModuleId = 2
       └─> Finds: v1.0.0 (old), v1.1.0 (new, latest)
T+2m   [System] Portal queries: SELECT * FROM ApplicationModules WHERE ModuleId = 2
       └─> Finds: App 5 uses VersionId=6 (v1.0.0)
T+2m   [System] Portal compares: v1.0.0 (current) < v1.1.0 (latest)
       └─> Sets status: "UpdateAvailable" ⚠️
T+5m   [Client] Logs into portal
T+5m   [Client] Opens Application Details page
T+5m   [UI] Displays: "Update Available: v1.0.0 → v1.1.0" 🟠
T+10m  [Client] Clicks "View Changelog"
T+10m  [Client] Reviews changes, sees breaking change warning ⚠️
T+15m  [Client] Clicks "Upgrade" button in portal
T+15m  [API] POST /api/upgrade/applications/5/modules/2/upgrade
T+15m  [DB] UPDATE ApplicationModules SET VersionId = 7 WHERE AppId = 5
T+15m  [UI] Status changes to: "Up to Date" 🟢
T+20m  [Client] Opens their codebase
T+20m  [Client] Checks: npm list primus-identity-validator
       └─> Output: v1.0.0 ⚠️ (still old)
T+25m  [Client] Updates package.json: "primus-identity-validator": "^1.1.0"
T+30m  [Client] Runs: npm install
T+31m  [Client] Checks: npm list primus-identity-validator
       └─> Output: v1.1.0 ✅ (now updated)
T+35m  [Client] Tests application locally
T+40m  [Client] Commits and deploys
T+60m  [Production] Application running with v1.1.0 ✅
```

---

## Breaking Changes Handling

### Detection

**In Portal Database**:
```sql
SELECT 
  m.Name AS ModuleName,
  mv.Version,
  mv.IsBreakingChange,
  mv.Changelog
FROM ModuleVersions mv
JOIN Modules m ON mv.ModuleId = m.Id
WHERE mv.IsBreakingChange = 1;
```

**Result**:
```
┌─────────────────────┬─────────┬──────────────────┬─────────────────┐
│ ModuleName          │ Version │ IsBreakingChange │ Changelog       │
├─────────────────────┼─────────┼──────────────────┼─────────────────┤
│ Identity Validator  │ 1.1.0   │ TRUE             │ requireRoles... │
└─────────────────────┴─────────┴──────────────────┴─────────────────┘
```

### UI Indicators

**Application Details Page**:
```
┌─────────────────────────────────────────────┐
│  Module: Identity Validator                 │
│  ⚠️  BREAKING CHANGE                        │
│  Current: v1.0.0 → Latest: v1.1.0           │
│                                             │
│  [⚠️  View Breaking Changes] [Upgrade]      │
└─────────────────────────────────────────────┘
```

**Upgrade Manager Page**:
```
┌─────────────┬─────────┬────────┬─────────────┬────────┐
│ Module      │ Current │ Latest │ Status      │ Actions│
├─────────────┼─────────┼────────┼─────────────┼────────┤
│ Identity    │ v1.0.0  │ v1.1.0 │ ⚠️ Breaking │[Review]│
│ Validator   │         │        │   Change    │        │
└─────────────┴─────────┴────────┴─────────────┴────────┘
```

### Migration Process

**Step 1: Review Migration Guide** (in changelog modal)
```
┌──────────────────────────────────────────┐
│  ⚠️  BREAKING CHANGE - Migration Guide   │
├──────────────────────────────────────────┤
│  The requireRoles() function signature   │
│  has changed to accept an array.         │
│                                          │
│  Before (v1.0.0):                        │
│    requireRoles('admin')                 │
│                                          │
│  After (v1.1.0):                         │
│    requireRoles(['admin'])               │
│                                          │
│  For multiple roles:                     │
│    requireRoles(['admin', 'moderator'])  │
│                                          │
│  Affected Files:                         │
│    src/middleware/auth.ts                │
│    src/routes/admin.ts                   │
└──────────────────────────────────────────┘
```

**Step 2: Search Codebase for Breaking Changes**
```bash
# Find all usages of old pattern
grep -r "requireRoles(" src/
```

**Output**:
```
src/routes/admin.ts:15:  app.get('/admin', requireRoles('admin'), ...
src/routes/users.ts:22:  app.post('/users', requireRoles('admin'), ...
src/routes/posts.ts:10:  app.delete('/posts', requireRoles('moderator'), ...
```

**Step 3: Update All Occurrences**
```typescript
// src/routes/admin.ts
// OLD: requireRoles('admin')
// NEW: requireRoles(['admin'])

app.get('/admin', requireRoles(['admin']), (req, res) => {
  // Handler code
});
```

**Step 4: Test Changes**
```bash
npm test  # Run unit tests
npm run lint  # Check for syntax errors
npm start  # Test locally
```

---

## Automation Opportunities

### Current Manual Process

```
❌ MANUAL STEPS (Time-Consuming):
1. Admin publishes npm package manually
2. Admin creates portal version manually (separate step)
3. Client logs into portal to check for updates
4. Client upgrades in portal (one-click ✅)
5. Client manually updates package.json
6. Client manually runs npm install
7. Client manually tests and deploys
```

### Future Automation Options

#### Option 1: Automated Version Synchronization

**Concept**: Portal automatically detects new npm versions

```typescript
// Backend service (runs every hour)
async function syncNpmVersions() {
  const modules = await db.modules.findAll();
  
  for (const module of modules) {
    // Check npm registry for new versions
    const response = await axios.get(`https://registry.npmjs.org/${module.npmPackageName}`);
    const latestVersion = response.data['dist-tags'].latest;
    
    // Compare with portal database
    const portalLatest = await db.moduleVersions.findOne({
      where: { moduleId: module.id },
      order: [['releasedAt', 'DESC']]
    });
    
    if (latestVersion !== portalLatest.version) {
      // Auto-create new version record
      await db.moduleVersions.create({
        moduleId: module.id,
        version: latestVersion,
        releaseNotes: response.data.versions[latestVersion].description,
        // Flag for admin review
        status: 'PendingReview'
      });
      
      // Notify admins
      await sendNotification('New version detected', latestVersion);
    }
  }
}
```

#### Option 2: Client Application Auto-Detection

**Concept**: Client application detects version mismatch at startup

```typescript
// Client application startup (server.ts)
import { checkVersionMismatch } from 'primus-identity-validator';

async function startServer() {
  // Check portal for expected version
  const portalVersion = await fetchPortalVersion(process.env.PRIMUS_CLIENT_ID);
  const installedVersion = require('primus-identity-validator/package.json').version;
  
  if (portalVersion !== installedVersion) {
    console.warn(`
      ⚠️  VERSION MISMATCH DETECTED
      Portal expects: v${portalVersion}
      Installed: v${installedVersion}
      
      To upgrade:
      1. Update package.json: "primus-identity-validator": "^${portalVersion}"
      2. Run: npm install
      3. Restart application
    `);
    
    // Optionally block startup
    if (process.env.STRICT_VERSION_CHECK === 'true') {
      throw new Error('Version mismatch - upgrade required');
    }
  }
  
  app.listen(3000);
}
```

#### Option 3: Automated Dependency Updates

**Concept**: Use Dependabot/Renovate to auto-update packages

**.github/renovate.json**:
```json
{
  "extends": ["config:base"],
  "packageRules": [
    {
      "matchPackageNames": ["primus-identity-validator"],
      "automerge": false,  // Requires manual review
      "labels": ["primus-saas", "security"],
      "prBodyNotes": [
        "⚠️ Check Primus Portal for breaking changes before merging",
        "Portal Upgrade Manager: https://portal.primus-saas.com/upgrade"
      ]
    }
  ]
}
```

**Result**: Automated PR created when new version is published
```
🤖 Renovate Bot
Update dependency primus-identity-validator to v1.1.0

Changes:
- primus-identity-validator: ^1.0.0 → ^1.1.0

⚠️ Breaking Change Detected
Check portal for migration guide before merging.

Portal Upgrade Manager: https://portal.primus-saas.com/upgrade

[View Changelog] [Merge]
```

#### Option 4: CI/CD Version Validation

**Concept**: CI pipeline fails if versions don't match

**.github/workflows/validate-versions.yml**:
```yaml
name: Validate Primus Versions

on: [push, pull_request]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Check Portal Version
        run: |
          PORTAL_VERSION=$(curl -H "Authorization: Bearer ${{ secrets.PRIMUS_TOKEN }}" \
            https://api.primus-saas.com/applications/${{ secrets.APP_ID }}/expected-versions)
          
          INSTALLED_VERSION=$(npm list primus-identity-validator --depth=0 --json | jq -r '.dependencies."primus-identity-validator".version')
          
          if [ "$PORTAL_VERSION" != "$INSTALLED_VERSION" ]; then
            echo "❌ Version mismatch: Portal=$PORTAL_VERSION, Installed=$INSTALLED_VERSION"
            exit 1
          fi
          
          echo "✅ Versions match: $PORTAL_VERSION"
```

---

## Summary

### Key Verification Points

| Stage | What to Verify | How to Verify | Expected Result |
|-------|----------------|---------------|-----------------|
| **Portal Registry** | Module version published | Check Modules page | Version visible in list |
| **Portal App Config** | App upgraded to new version | Check Application Details | Status: "Up to Date" |
| **npm Registry** | Package published | `npm view primus-identity-validator versions` | Version in list |
| **Local Installation** | Package downloaded | `npm list primus-identity-validator` | Correct version shown |
| **File System** | Package files present | Check `node_modules/` folder | Version matches |
| **Build Process** | Code compiles | `npm run build` | Build succeeds |
| **Runtime** | Correct version loaded | Check logs/health endpoint | Version displayed |
| **Functionality** | Features work | Test authentication | Endpoints respond correctly |

### Common Issues & Solutions

#### Issue 1: Portal shows v1.1.0, but npm install gets v1.0.0

**Cause**: npm cache is stale

**Solution**:
```bash
npm cache clean --force
npm install primus-identity-validator@1.1.0
```

#### Issue 2: Build fails after upgrading

**Cause**: Breaking changes not addressed

**Solution**:
1. Check changelog in portal
2. Review migration guide
3. Update code per guide
4. Test changes

#### Issue 3: Portal says "Up to Date" but package is old

**Cause**: Client never updated package.json

**Solution**:
1. Portal tracks *intended* version
2. Code must be manually updated
3. Run `npm install` after updating package.json

#### Issue 4: Multiple applications on different versions

**Cause**: Each app tracks its own version independently

**Solution**: This is by design
- App A can use v1.0.0
- App B can use v1.1.0
- Upgrade each app individually

---

## Next Steps

### For Clients

1. ✅ Check Application Details page regularly for updates
2. ✅ Subscribe to module update notifications (future feature)
3. ✅ Review changelogs before upgrading
4. ✅ Test upgrades in staging before production
5. ✅ Keep package.json in sync with portal

### For Platform Admins

1. ✅ Document breaking changes clearly
2. ✅ Provide migration guides
3. ✅ Notify clients before breaking changes
4. ✅ Consider automated version sync
5. ✅ Monitor upgrade adoption rates

---

**Document End**

For questions or support, contact: support@primus-saas.com
