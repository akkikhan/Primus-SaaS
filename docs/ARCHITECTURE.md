# Primus SaaS Platform – Architecture & Design Decisions

**Version**: 1.1  
**Last Updated**: November 15, 2025

---

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Data Flow & Isolation](#data-flow--isolation)
3. [Update Awareness & Version Management](#update-awareness--version-management)
4. [Token Validation: Why & How](#token-validation-why--how)
5. [SDK vs Code Snippets](#sdk-vs-code-snippets)
6. [Security Model](#security-model)

---

## 1. System Architecture

### High-Level Components

```text
┌─────────────────────────────────────────────────────────────────┐
│                    Primus SaaS Platform                         │
│                                                                 │
│  ┌───────────────────────────────────────────────────────┐    │
│  │              Portal (Internal Admin Tool)              │    │
│  │  ┌─────────────┐         ┌──────────────┐            │    │
│  │  │   Backend   │◄────────┤   Frontend   │            │    │
│  │  │  (.NET 8)   │         │ (React + TS) │            │    │
│  │  └─────────────┘         └──────────────┘            │    │
│  │                                                        │    │
│  │  Functions:                                           │    │
│  │  • Module catalog management                          │    │
│  │  • Application registry                               │    │
│  │  • Documentation generation                           │    │
│  │  • Version tracking & upgrade notifications           │    │
│  └───────────────────────────────────────────────────────┘    │
│                                                                 │
│  ┌───────────────────────────────────────────────────────┐    │
│  │            SDK Modules (Published Packages)            │    │
│  │                                                        │    │
│  │  📦 Primus.SaaS.IdentityValidator (NuGet)            │    │
│  │  📦 @primus-saas/identity-validator (NPM)            │    │
│  │                                                        │    │
│  │  Capabilities:                                         │    │
│  │  • Azure AD JWT validation                            │    │
│  │  • Local JWT generation & validation                  │    │
│  │  • Authentication middleware                          │    │
│  └───────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Documentation (Email/PDF)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Client Application                           │
│                                                                 │
│  ┌───────────────────────────────────────────────────────┐    │
│  │                     Backend API                        │    │
│  │              (.NET, Node, Express, NestJS)             │    │
│  │                                                        │    │
│  │  ┌────────────────────────────────────────────┐      │    │
│  │  │  Primus SDK (installed via NuGet/NPM)      │      │    │
│  │  │  • Validates tokens                         │      │    │
│  │  │  • Issues local JWTs                        │      │    │
│  │  │  • No external calls to Primus              │      │    │
│  │  └────────────────────────────────────────────┘      │    │
│  │                                                        │    │
│  │  ┌────────────────────────────────────────────┐      │    │
│  │  │          Client's Own Database              │      │    │
│  │  │  • Users, passwords, roles                  │      │    │
│  │  │  • All business data                        │      │    │
│  │  └────────────────────────────────────────────┘      │    │
│  └───────────────────────────────────────────────────────┘    │
│                                                                 │
│  ┌───────────────────────────────────────────────────────┐    │
│  │                  Frontend (SPA/Web)                    │    │
│  │                                                        │    │
│  │  For Azure AD:                                         │    │
│  │  • Uses MSAL to get tokens from Azure AD              │    │
│  │  • Sends token to backend API                         │    │
│  │                                                        │    │
│  │  For Local Auth:                                       │    │
│  │  • Sends username/password to backend                 │    │
│  │  • Receives JWT from backend                          │    │
│  └───────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │
                    (Azure AD tokens only)
                              │
┌─────────────────────────────────────────────────────────────────┐
│                       Azure AD / Entra ID                       │
│                                                                 │
│  • Issues tokens for users                                     │
│  • Provides JWKS (public keys) for validation                 │
│  • Client's own Azure AD tenant                                │
└─────────────────────────────────────────────────────────────────┘
```

### Key Architectural Principles

1. **No Runtime Dependency on Primus**
   - Portal is only used during setup/configuration
   - SDK runs entirely within client infrastructure
   - No API calls to Primus servers during authentication

2. **Complete Data Isolation**
   - Tokens never leave client environment
   - User data stored only in client's database
   - Primus only stores metadata (app names, module versions)

3. **Standard Package Distribution**
   - SDKs distributed via public registries (NuGet, NPM)
   - Clients update packages using standard tooling
   - No custom package managers or updaters

---

## 2. Data Flow & Isolation

### Authentication Flow: Azure AD Mode

```text
┌─────────────┐                           ┌──────────────┐
│  End User   │                           │  Azure AD    │
│  (Browser)  │                           │              │
└──────┬──────┘                           └──────┬───────┘
       │                                          │
       │ 1. Login redirect                        │
       ├─────────────────────────────────────────►│
       │                                          │
       │ 2. User authenticates                    │
       │                                          │
       │ 3. ID Token + Access Token               │
       │◄─────────────────────────────────────────┤
       │                                          │
       │                                          │
       ▼                                          │
┌────────────────────────────┐                   │
│  Client Frontend (SPA)     │                   │
│                            │                   │
│  Stores token in memory    │                   │
└────────────┬───────────────┘                   │
             │                                    │
             │ 4. API call with                   │
             │    Authorization: Bearer <token>   │
             │                                    │
             ▼                                    │
┌──────────────────────────────────────────┐     │
│  Client Backend API                      │     │
│                                          │     │
│  ┌───────────────────────────────────┐  │     │
│  │  Primus SDK (runs in-process)     │  │     │
│  │                                   │  │     │
│  │  5. Validates token:              │  │     │
│  │     • Fetches JWKS from Azure AD  │  ├─────┘
│  │       (public keys, cached)       │  │
│  │     • Verifies signature          │  │
│  │     • Checks issuer, audience     │  │
│  │     • Validates expiry            │  │
│  │                                   │  │
│  │  6. Extracts claims (user ID,     │  │
│  │     roles, email, etc.)           │  │
│  └───────────────────────────────────┘  │
│                                          │
│  7. Business logic with authenticated    │
│     user context                         │
│                                          │
│  ┌───────────────────────────────────┐  │
│  │  Client's Database                │  │
│  │  • User profiles                  │  │
│  │  • Business data                  │  │
│  └───────────────────────────────────┘  │
└──────────────────────────────────────────┘

Note: Primus SaaS Platform servers are NOT in this flow.
Token validation happens entirely in client infrastructure.
```

### Authentication Flow: Local Mode

```text
┌─────────────┐
│  End User   │
│  (Browser)  │
└──────┬──────┘
       │
       │ 1. POST /auth/login
       │    { username, password }
       │
       ▼
┌────────────────────────────┐
│  Client Backend API        │
│                            │
│  /auth/login endpoint      │
│                            │
│  2. Query client's DB      │
│     for user               │
│                            │
│  ┌──────────────────────┐ │
│  │  Client's Database   │ │
│  │  • Users table       │ │
│  │  • Password hashes   │ │
│  └──────────────────────┘ │
│                            │
│  3. Verify password hash   │
│                            │
│  ┌──────────────────────┐ │
│  │  Primus SDK          │ │
│  │                      │ │
│  │  4. Generate JWT     │ │
│  │     with claims      │ │
│  │     (user ID, roles) │ │
│  │                      │ │
│  │  5. Sign with client's│ │
│  │     secret key       │ │
│  └──────────────────────┘ │
│                            │
│  6. Return JWT to frontend│
└────────────┬───────────────┘
             │
             │ 7. JWT stored in
             │    frontend (cookie/
             │    localStorage)
             ▼
┌────────────────────────────┐
│  Client Frontend           │
│                            │
│  8. Subsequent API calls   │
│     include JWT in         │
│     Authorization header   │
└────────────┬───────────────┘
             │
             │ 9. API call with Bearer token
             │
             ▼
┌──────────────────────────────────────────┐
│  Client Backend API                      │
│                                          │
│  ┌───────────────────────────────────┐  │
│  │  Primus SDK                       │  │
│  │                                   │  │
│  │  10. Validates JWT:               │  │
│  │      • Verifies signature         │  │
│  │        (using client's secret)    │  │
│  │      • Checks issuer, audience    │  │
│  │      • Validates expiry           │  │
│  │                                   │  │
│  │  11. Extracts claims              │  │
│  └───────────────────────────────────┘  │
│                                          │
│  12. Business logic with authenticated   │
│      user context                        │
└──────────────────────────────────────────┘

Note: All data (users, passwords, tokens) stays
within client's infrastructure.
Primus SaaS Platform is never involved.
```

### Data Isolation Guarantees

| Data Type | Stored in Client DB | Processed by Client Backend | Sent to Primus | Processed by Primus SDK |
|-----------|--------------------|-----------------------------|----------------|------------------------|
| User credentials (username/password) | ✅ | ✅ | ❌ | ❌ |
| Password hashes | ✅ | ✅ | ❌ | ❌ |
| JWT tokens (local) | ❌ (ephemeral) | ✅ | ❌ | ✅ (validation) |
| Azure AD tokens | ❌ (ephemeral) | ✅ | ❌ | ✅ (validation) |
| User profile data | ✅ | ✅ | ❌ | ❌ |
| Business data | ✅ | ✅ | ❌ | ❌ |
| Application metadata | ❌ | ❌ | ✅ | ❌ |
| Module versions | ❌ | ❌ | ✅ | ❌ |
| PrimusClientId | ❌ | ✅ (config) | ✅ | ✅ (validation) |

**Key Insight**: Primus SDK runs *inside* the client's application process. It never sends data out. The SDK is essentially a library that provides authentication logic as code.

---

## 3. Update Awareness & Version Management

### Problem Statement

**Question**: How do client applications know when a new version of a module is available? How do they update?

### Solution: Multi-Channel Awareness

```text
┌──────────────────────────────────────────────────────────────┐
│                  New Module Version Released                  │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────┐
│  Admin Updates Module Version in Portal                      │
│  • Version: 1.2.0                                            │
│  • Breaking: false                                            │
│  • Release notes: "Added support for custom claims"          │
└────────────────────────────┬─────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌──────────────┐   ┌──────────────────┐   ┌─────────────────┐
│ Portal View  │   │ Email Campaign   │   │ GitHub Release  │
│              │   │                  │   │                 │
│ Shows:       │   │ Template:        │   │ Public release  │
│ • Apps using │   │                  │   │ notes + CHANGELOG│
│   v1.0.0     │   │ Subject:         │   │                 │
│ • v1.2.0     │   │ "IdentityValidator│   │ Clients can     │
│   available  │   │  v1.2.0 Released"│   │ subscribe to    │
│ • "Upgrade   │   │                  │   │ notifications   │
│   Available" │   │ Body:            │   │                 │
│   badge      │   │ • What's new     │   └─────────────────┘
│              │   │ • Migration steps│
│ Admin sees   │   │ • Install cmd    │
│ which clients│   │                  │
│ need updates │   │ Admin sends to   │
│              │   │ client devs      │
└──────────────┘   └──────────────────┘
```

#### Channel 1: Portal Dashboard (Admin View)

When an admin logs into the portal:

```text
┌─────────────────────────────────────────────────────────────┐
│  Applications                                                │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  📱 Acme Claims API                                          │
│     Stack: .NET                                              │
│     Modules:                                                 │
│     • IdentityValidator: v1.0.0  ⚠️ UPDATE AVAILABLE (v1.2.0)│
│                                                              │
│     [View Details] [Generate Upgrade Doc]                    │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  📱 Contoso Inventory Service                                │
│     Stack: Node                                              │
│     Modules:                                                 │
│     • IdentityValidator: v1.2.0  ✅ UP TO DATE               │
│                                                              │
│     [View Details]                                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

#### Channel 2: Email Notifications

Portal generates email templates:

```text
From: Primus SaaS Platform <admin@primus.com>
To: dev-team@acmecorp.com
Subject: IdentityValidator v1.2.0 Released - Upgrade Recommended

Hi Acme Claims API team,

A new version of IdentityValidator is now available.

📦 Module: IdentityValidator
📊 Current Version: 1.0.0
🆕 New Version: 1.2.0
🔴 Breaking Changes: No

What's New:
• Added support for custom JWT claims
• Improved Azure AD token cache performance
• Bug fix: Expiry validation edge case

Upgrade Instructions:

For .NET:
dotnet add package Primus.SaaS.IdentityValidator --version 1.2.0

For Node:
npm install @primus-saas/identity-validator@1.2.0

Migration Steps:
No breaking changes. Simply update the package version and rebuild.

Full release notes: https://github.com/primus/identity-validator/releases/v1.2.0

Questions? Reply to this email.

Best,
Primus SaaS Platform Team
```

#### Channel 3: GitHub Releases & CHANGELOG

Each module repository has:

- **GitHub Releases**: Tagged versions with notes
- **CHANGELOG.md**: Comprehensive change log
- **NPM/NuGet Registry**: Version history visible to clients

Developers can:

- Subscribe to GitHub release notifications
- Check npm/NuGet for available versions
- Use `npm outdated` or similar tools

### Update Process (Client Side)

```text
┌──────────────────────────────────────────────────────────────┐
│  Client Developer Receives Notification                      │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────┐
│  Review Release Notes & Migration Instructions               │
│  • Is it a breaking change?                                  │
│  • What config/code changes are needed?                      │
│  • Is there urgency (security fix)?                          │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────┐
│  Update Package Version                                       │
│                                                              │
│  .NET:                                                        │
│  • Update .csproj:                                           │
│    <PackageReference Include="Primus.SaaS.IdentityValidator"│
│                      Version="1.2.0" />                      │
│  • Or: dotnet add package Primus.SaaS.IdentityValidator     │
│        --version 1.2.0                                       │
│                                                              │
│  Node:                                                        │
│  • Update package.json:                                      │
│    "@primus-saas/identity-validator": "^1.3.0"              │
│  • Or: npm install @primus-saas/identity-validator@1.3.0    │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────┐
│  Apply Migration Changes (if any)                            │
│  • Update configuration (if new options added)               │
│  • Update code (if breaking changes)                         │
└────────────────────────────┬─────────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────────┐
│  Test, Build, Deploy                                          │
│  • Run unit & integration tests                              │
│  • Deploy to staging                                         │
│  • Validate authentication still works                       │
│  • Deploy to production                                      │
└──────────────────────────────────────────────────────────────┘
```

### No Magic Required

**Key Point**: There is **no special Primus updater** or custom command. Clients use standard package management:

- **NuGet**: `dotnet add package`, `dotnet restore`, or edit `.csproj`
- **NPM**: `npm install`, `npm update`, or edit `package.json`

Primus's job is to:

1. Make awareness easy (email, portal view, GitHub)
2. Provide clear migration instructions
3. Track which apps are on which versions

---

## 4. Token Validation: Why & How

### The Problem: Why Validate Tokens?

#### Scenario: No Validation (Insecure)

```text
┌─────────────┐
│  Attacker   │
└──────┬──────┘
       │
       │ Crafts fake token:
       │ eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.
       │ eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJhZG1pbiJ9.
       │
       ▼
┌──────────────────────────────────────────┐
│  Client Backend API (NO VALIDATION)      │
│                                          │
│  app.get("/api/admin", (req, res) => {  │
│    const token = req.headers.authorization;│
│    const decoded = jwt.decode(token);    │  ⚠️ DANGER
│    // No signature check!                │
│    if (decoded.role === "admin") {       │
│      return res.json(secretData);        │  ❌ Breach
│    }                                      │
│  });                                      │
└──────────────────────────────────────────┘
```

**Attacker can:**

- Create their own JWT with `role: "admin"`
- Use `alg: "none"` (no signature)
- Send to API and gain unauthorized access

#### Scenario: With Validation (Secure)

```text
┌─────────────┐
│  Attacker   │
└──────┬──────┘
       │
       │ Sends fake token
       │
       ▼
┌──────────────────────────────────────────┐
│  Client Backend API (WITH VALIDATION)    │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  Primus SDK                        │ │
│  │                                    │ │
│  │  1. Parse token header             │ │
│  │  2. Fetch signing key (JWKS or    │ │
│  │     secret)                        │ │
│  │  3. Verify signature               │ │
│  │     ❌ Signature invalid!           │ │
│  │  4. Return 401 Unauthorized        │ │
│  └────────────────────────────────────┘ │
│                                          │
│  Attacker blocked ✅                     │
└──────────────────────────────────────────┘
```

### What We Validate

#### For Azure AD Tokens

```typescript
// Token structure
{
  "header": {
    "alg": "RS256",
    "kid": "key-id-from-azure"
  },
  "payload": {
    "iss": "https://login.microsoftonline.com/{tenant}/v2.0",
    "aud": "api://your-app-id",
    "sub": "user-guid",
    "exp": 1731700000,
    "nbf": 1731696400,
    "name": "John Doe",
    "roles": ["Admin", "User"]
  },
  "signature": "..." // Signed by Azure AD's private key
}
```

**Validation Steps**:

1. **Signature**
   - Fetch Azure AD's public keys (JWKS endpoint)
   - Verify token was signed by Azure AD's private key
   - Ensures token wasn't tampered with

2. **Issuer (`iss`)**
   - Must match expected Azure AD tenant
   - Prevents tokens from other tenants being used

3. **Audience (`aud`)**
   - Must match your API's client ID
   - Prevents tokens meant for other apps being used

4. **Expiry (`exp`)**
   - Token must not be expired
   - Prevents replay attacks with old tokens

5. **Not Before (`nbf`)**
   - Token must not be used before this time
   - Prevents time-based attacks

6. **Algorithm (`alg`)**
   - Must be RS256 or other secure algorithm
   - Prevents `alg: none` vulnerability

#### For Local Tokens

```typescript
// Token structure
{
  "header": {
    "alg": "HS256", // Or RS256 if using asymmetric keys
    "typ": "JWT"
  },
  "payload": {
    "iss": "your-app",
    "aud": "your-app-users",
    "sub": "user-123",
    "exp": 1731700000,
    "username": "johndoe",
    "roles": ["user"]
  },
  "signature": "..." // Signed by your secret key
}
```

**Validation Steps**:

1. **Signature**
   - Verify using your secret signing key (from config)
   - Ensures token was issued by your app

2. **Issuer & Audience**
   - Must match your app's config
   - Prevents cross-app token reuse

3. **Expiry**
   - Standard expiry check

4. **Custom Claims**
   - Extract user ID, roles, etc.
   - Use for authorization logic

### Without Validation: Attack Scenarios

#### Attack 1: Token Forgery

```text
Attacker decodes your token, changes "role": "user" to "role": "admin",
re-encodes it, and sends to API.

Without signature verification → ✅ Attacker succeeds
With signature verification → ❌ Signature mismatch, rejected
```

#### Attack 2: Token Replay from Different App

```text
Attacker obtains valid token from App A, sends to App B.

Without audience check → ✅ Token accepted (wrong app!)
With audience check → ❌ Audience mismatch, rejected
```

#### Attack 3: Expired Token Reuse

```text
Attacker uses 6-month-old token from leaked database.

Without expiry check → ✅ Token accepted (user may no longer exist!)
With expiry check → ❌ Token expired, rejected
```

#### Attack 4: Algorithm Confusion

```text
Attacker changes token algorithm from RS256 to "none", removes signature.

Without algorithm validation → ✅ Token accepted (no signature!)
With algorithm validation → ❌ Unsupported algorithm, rejected
```

### Why Primus SDK Handles This

**Option 1: SDK Does Validation (Recommended)**

```csharp
// Client's code (simple)
builder.Services.AddPrimusIdentityValidator(options => {
    options.AzureAd.TenantId = "...";
    options.AzureAd.ClientId = "...";
});

app.UseAuthentication(); // SDK handles all validation
```

**Benefits**:

- Validation logic is correct, tested, secure
- Consistent across all client apps
- Easy to update if vulnerabilities found
- Client devs focus on business logic

**Option 2: Client Implements Validation (Not Recommended)**

```csharp
// Client must write:
// - JWKS fetching & caching
// - Signature verification
// - Claims validation
// - Error handling
// - Key rotation logic
// 100+ lines of security-critical code per client
```

**Risks**:

- Easy to get wrong (security vulnerabilities)
- Inconsistent implementations
- Hard to fix bugs across all clients
- No leverage of Primus expertise

**Verdict**: SDK provides validation as a *service via code*, not as a SaaS API call. This maintains:

- ✅ Zero runtime dependency on Primus servers
- ✅ Complete data isolation
- ✅ Security best practices
- ✅ Ease of use

---

## 5. SDK vs Code Snippets

### Comparison Matrix

| Aspect | Full SDK (Current Design) | Code Snippets Only | Hybrid |
|--------|---------------------------|-------------------|--------|
| **Distribution** | NuGet/NPM packages | Docs, GitHub gists, copy-paste | SDK + reference code |
| **Versioning** | SemVer, trackable | Docs versioned, but no package tracking | SDK versioned, snippets as examples |
| **Updates** | `dotnet add package --version X` | Manual code replacement | SDK updates standard, snippets manual |
| **Security** | Centralized, tested, reviewed | Each client implements differently | SDK secure, snippets for reference |
| **Consistency** | Same behavior across all clients | Varies by implementation | SDK consistent, snippets show internals |
| **Ease of Use** | High (plug & play) | Low (requires security expertise) | Medium (SDK for most, snippets for custom) |
| **Data Isolation** | ✅ Perfect (runs in-process) | ✅ Perfect (client's own code) | ✅ Perfect (both in-process) |
| **Maintenance** | Primus fixes bugs, clients update | Each client maintains own code | Primus SDK maintained, snippets static |
| **Testability** | SDK includes tests | Client must write tests | SDK tested, snippets as-is |
| **Support** | Clear issue tracking (GitHub issues) | Hard to support (code everywhere) | SDK supportable, snippets not |

### Decision: Full SDK (Recommended)

**Why SDK Wins**:

1. **Security**: Critical validation logic is easy to get wrong. SDK ensures correctness.
2. **Consistency**: All clients use same tested implementation.
3. **Updates**: Bug fixes and security patches distributed via standard package updates.
4. **Developer Experience**: Easier integration = faster adoption.
5. **Product Value**: SDK is a product; snippets are just documentation.

**Data Isolation is Preserved**:

- SDK runs in-process in client's backend
- No network calls to Primus
- All tokens/data stay local
- SDK is essentially "production-ready authentication code as a library"

### Hybrid Approach

**Best of Both Worlds**:

1. Ship SDK as primary integration method
2. Publish SDK source code on GitHub (open or shared with clients)
3. Include "How It Works" docs explaining validation logic
4. Allow clients to fork SDK if they need customizations

This gives:

- **90% of clients**: Use SDK, get security & ease of use
- **10% of clients**: Can audit or customize if needed
- **Primus**: Can demonstrate transparency while maintaining product quality

---

## 6. Security Model

### Threat Model

| Threat | Mitigation |
|--------|-----------|
| **Token Forgery** | Signature verification (SDK validates with public keys or secrets) |
| **Token Replay (Expired)** | Expiry (`exp`) validation |
| **Token Reuse (Cross-App)** | Audience (`aud`) validation |
| **Token Reuse (Cross-Tenant)** | Issuer (`iss`) validation |
| **Algorithm Confusion** | Algorithm whitelist (only RS256/HS256) |
| **Man-in-the-Middle** | HTTPS required (not enforced by SDK, but documented) |
| **Leaked Signing Keys** | Key rotation (client's responsibility, docs provided) |
| **Compromised Client Infrastructure** | Out of scope (client infrastructure security is client's responsibility) |
| **Compromised Primus Portal** | Portal only stores metadata; no tokens or user data at risk |

### Trust Boundaries

```text
┌───────────────────────────────────────────────────────────────┐
│                        Trust Boundary 1:                       │
│                     Client Infrastructure                       │
│                                                                │
│  ✅ Client owns:                                               │
│     • Backend servers                                          │
│     • Databases                                                │
│     • Signing keys                                             │
│     • User data                                                │
│     • Tokens (in memory/transit only)                         │
│                                                                │
│  ✅ Primus SDK runs here (in-process, no network calls)       │
│                                                                │
│  ❌ Primus servers CANNOT access:                              │
│     • Tokens                                                   │
│     • User data                                                │
│     • Passwords                                                │
│     • Business data                                            │
└───────────────────────────────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────┐
│                        Trust Boundary 2:                       │
│                   Primus SaaS Platform Portal                  │
│                                                                │
│  ✅ Primus owns:                                               │
│     • Portal servers                                           │
│     • Module catalog database                                  │
│     • Application metadata                                     │
│                                                                │
│  ✅ Primus stores:                                             │
│     • Application names                                        │
│     • PrimusClientId                                           │
│     • Module versions in use                                   │
│     • Admin credentials                                        │
│                                                                │
│  ❌ Primus NEVER stores:                                       │
│     • Client end-user credentials                              │
│     • Tokens                                                   │
│     • PII (personal identifiable information)                  │
│     • Business data                                            │
└───────────────────────────────────────────────────────────────┘
```

### Compliance & Privacy

**Data Residency**:

- All user data stays in client's infrastructure
- Clients control data residency (on-prem, cloud, region)
- Primus has no access to end-user data

**GDPR/Privacy**:

- Primus is NOT a data processor for end-user data
- Clients are data controllers
- No DPA (Data Processing Agreement) needed for end-user data
- Primus may have DPA for application metadata (if needed)

**Audit Trail**:

- Portal logs admin actions (module releases, app creation)
- Clients log authentication events in their systems
- No centralized user activity tracking by Primus

---

## Summary

### Why This Architecture?

1. **Zero Runtime Dependency**: Clients never depend on Primus availability for auth to work
2. **Data Isolation**: All sensitive data stays in client infrastructure
3. **Standard Tooling**: Uses npm/NuGet, no custom updaters
4. **Security First**: SDK provides tested, secure validation logic
5. **Scalability**: No Primus servers in hot path; clients scale independently
6. **Trust**: Clients maintain full control; Primus provides tools, not SaaS

### What Primus Provides

- **Portal**: Admin tool for managing modules and generating docs
- **SDK**: Production-ready authentication libraries
- **Documentation**: Migration guides, release notes, best practices
- **Support**: Issue tracking, security advisories

### What Clients Own

- **Runtime**: All authentication happens in their backend
- **Data**: All user data, tokens, business logic
- **Security**: Infrastructure security, key management
- **Scaling**: Independent scaling, no Primus bottleneck

---

**Next Steps**: See [PRD.md](./PRD.md) for detailed requirements and [FAQ.md](./FAQ.md) for common questions.
