# Primus SaaS Platform

**Version**: Identity (Node) 1.3.3 · Identity (.NET) 1.5.0 · Logging (Node) 1.2.4 / (.NET) 1.2.4  
**Type**: Developer Platform for reusable backend modules (Identity + Logging)

---

## Overview

Primus SaaS Platform ships reusable backend modules as NuGet and npm packages (Identity Validator + Logging) for Node.js and .NET. All logic runs inside your app—no Primus-hosted runtime or PII storage.

### Key Principles

- **Zero Runtime Dependency**: All authentication logic runs inside the client's application backend
- **No PII Storage**: Primus never stores or processes end-user data or tokens
- **Client-Side Integration**: Modules are integrated as packages in client applications
- **Admin Control Plane**: Primus Portal is an internal tool for managing applications and generating integration docs

---

## Repository Structure

```text
primus-saas-platform/
├── portal/                      # Internal control plane (ASP.NET Core API + React SPA)
├── sdk/                         # Published SDKs
│   ├── nodejs/primus-identity-validator
│   ├── logging/nodejs
│   ├── dotnet/PrimusSaaS.Identity.Validator
│   └── logging/dotnet
├── docs-site/                   # Docusaurus public docs
├── docs/                        # Product/architecture docs
├── examples/                    # Example client applications
└── test-apps/                   # Validation apps and scripts
```

---

## Components

### 1. Primus SaaS Platform Portal

Internal web application for:

- **Platform Admins**: Manage module catalog, versions, and client application entries

**Tech Stack**:

- Backend: ASP.NET Core 8 Web API
- Frontend: React + TypeScript
- Database: SQL Server / PostgreSQL (via EF Core)

**Note**: Client developers do not log into the portal in v1. Admins generate Documentation bundles and share them with clients via email/PDF.

### 2. SDK Modules

- **Identity Validator** — Multi-issuer JWT/OIDC validation with RBAC (Azure AD + Local JWT).  
  - npm: `@primus-saas/identity-validator@1.3.2`  
  - NuGet: `PrimusSaaS.Identity.Validator` 1.5.0
- **Logging Module** — Structured logging with enrichment, correlation IDs, timers, file/AI targets, PII masking.  
  - npm: `@primus-saas/logging@1.2.4`  
  - NuGet: `PrimusSaaS.Logging` 1.2.4

🚀 **Quick install**
```bash
# Identity
npm install @primus-saas/identity-validator
dotnet add package PrimusSaaS.Identity.Validator --version 1.5.0

# Logging
npm install @primus-saas/logging
dotnet add package PrimusSaaS.Logging --version 1.2.4
```

For detailed integration steps, see the module quick starts under `docs-site/docs/modules/` (Identity, Logging, Notifications) and the example apps under `examples/`.

---

## Quick Start

### For Platform Admins

1. Run the Portal backend and frontend
2. Log in as admin
3. Add module versions to the catalog
4. Create Application entries for client apps
5. Generate Documentation bundle for each application
6. Share Documentation with client developers (email, PDF, Confluence)

### For Client Developers

1. Receive Documentation from Primus admin
2. Note your assigned `PrimusClientId` and credentials
3. Install the SDK packages:

   ```bash
   # Identity
   npm install @primus-saas/identity-validator
   dotnet add package PrimusSaaS.Identity.Validator --version 1.5.0

   # Logging
   npm install @primus-saas/logging
   dotnet add package PrimusSaaS.Logging --version 1.2.4
   ```

4. Configure authentication using provided Portal URL, ClientId, and JwtSecret
5. Wire up middleware in your backend - see [.NET Example](examples/dotnet-api/README.md) or [Node.js Example](examples/nodejs-express/README.md) for complete integration guides

📚 **New to the SDKs?** Check out:
- [.NET SDK Documentation](sdk/dotnet/PrimusSaaS.Identity.Validator/README.md)
- [Node.js SDK Documentation](sdk/nodejs/primus-identity-validator/README.md)
- [CHANGELOG](CHANGELOG.md) - What's new in v1.0.0

---

## User Roles

| Role | Access | Responsibilities |
|------|--------|------------------|
| **Platform Admin** | Portal (internal) | Manage module catalog, versions, application entries, generate Documentation |
| **Client Developer** | No portal access (v1) | Receives Documentation from admin, integrates modules into their apps |
| **End User** | Client application only | Logs into client app; never interacts with Primus |

---

## Versioning

All modules follow **Semantic Versioning** (MAJOR.MINOR.PATCH):

- **PATCH**: Bug fixes (no breaking changes)
- **MINOR**: New features (backward-compatible)
- **MAJOR**: Breaking changes

Portal tracks module versions per client application and provides upgrade guidance via email notifications and migration docs.

---

## 🐳 Quick Start with Docker

The easiest way to run the entire Primus Portal stack locally:

```bash
# 1. Copy environment template
cp .env.example .env

# 2. Start all services (Database + Backend + Frontend)
docker-compose up -d

# 3. Access the portal
# Frontend: http://localhost:5173
# Backend API: http://localhost:5267
# Default credentials: admin@primussaas.com / Admin123!
```

**Services Included**:
- SQL Server 2022 (with automatic health checks)
- Portal Backend (.NET 8 Web API)
- Portal Frontend (React SPA with nginx)

See [DOCKER_SETUP.md](./DOCKER_SETUP.md) for detailed configuration and troubleshooting.

---

## Development

See individual README files in each component directory:

- [Portal Backend](./portal/backend/README.md)
- [Portal Frontend](./portal/frontend/README.md)
- [Identity Validator .NET SDK](./sdk/dotnet/PrimusSaaS.Identity.Validator/README.md)
- [Identity Validator Node.js SDK](./sdk/nodejs/primus-identity-validator/README.md)
- [.NET API Example](./examples/dotnet-api/README.md)
- [Node.js Express Example](./examples/nodejs-express/README.md)

---

## Azure AD Configuration

See the module quick starts (`docs-site/docs/modules/*-quick-start.md`) for Azure AD registration, multi-issuer (Azure AD + Local JWT) setup, and ready-to-run Node.js/.NET samples. Example projects live in `examples/`.

### Configuring Azure AD Mode

- **.NET:** Configure issuers in `PrimusIdentity` and wire `AddPrimusIdentity` + `UseAuthentication`. See integration guide samples for multi-issuer (Azure + Local) config.
- **Node.js:** Pass issuers to `primusIdentityMiddleware` from `@primus-saas/identity-validator`; use Azure AD authority/issuer and your API audience. See the integration guide for the full Express/Nest snippets.

### Setting Up Azure AD App Registration

1. **Navigate to Azure Portal**: [https://portal.azure.com](https://portal.azure.com)
2. Go to **Azure Active Directory** → **App registrations** → **New registration**
3. **Register Application**:
   - **Name**: Your application name
   - **Supported account types**: Choose based on requirements (single/multi-tenant)
   - **Redirect URI**: Configure as needed for your auth flow
4. **Obtain Tenant ID**:
   - Go to **Overview** tab after registration
   - Copy the **Directory (tenant) ID** (GUID format)
   - Use this value in your SDK configuration
5. **Configure API Permissions** (if needed):
   - Add required permissions for your application
   - Grant admin consent if necessary

### Configuration Options

- **Mode**: Authentication mode
  - `Local`: JWT validation with symmetric keys (HS256)
  - `AzureAd`: JWKS validation with Azure AD public keys (RS256)
  - `Hybrid`: Support both modes simultaneously
- **TenantId**: Your Azure AD tenant ID (GUID or domain name) - **REQUIRED** for Azure AD mode
- **JwksCacheTtl**: JWKS cache duration in hours - **OPTIONAL** (default: 24 hours)

### Testing Azure AD Integration

Use Azure CLI to get a test token:

```powershell
# Login to Azure
az login

# Get access token
$token = az account get-access-token --query accessToken -o tsv

# Use the token in your API requests
curl -H "Authorization: Bearer $token" https://your-api.com/api/protected
```

For more details, see:
- [.NET Example with Azure AD](./examples/dotnet-api/README.md)
- [Node.js Example with Azure AD](./examples/nodejs-express/README.md)
- [Test Apps README](./test-apps/README.md)

---

## Terminology & Naming Standards

**⚠️ Important**: This project uses consistent terminology to avoid confusion.

### Correct Terms

- **Documentation** - The integration guide generated for client applications
- **Documentation Page** - The portal UI for viewing integration guides
- **Documentation Bundle** - The PDF/email artifact sent to clients

### Deprecated Terms (DO NOT USE)

- ~~Folio~~ - Renamed to "Documentation" as of v1.1
- ~~Folio Page~~ - Now "Documentation Page"
- ~~Folio Docs~~ - Now "Documentation Bundle"

### Regression Checklist for Contributors

Before creating new code or documentation:

1. ✅ Use "Documentation" terminology in all code, comments, and docs
2. ✅ Controllers/services should reference `Documentation` not `Folio`
3. ✅ UI components should use `DocumentationPage`, `documentationService`, etc.
4. ✅ Search codebase for "Folio" before committing to catch any drift
5. ✅ Update this checklist if new naming conventions are established

---

## License

Proprietary - Primus SaaS Platform
