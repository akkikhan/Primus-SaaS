# Primus SaaS Platform

**Version**: 1.1 MVP  
**Type**: Developer Platform for Reusable Backend Modules

---

## Overview

Primus SaaS Platform is a developer-focused platform that provides horizontally scoped, reusable backend modules as NuGet and NPM packages. The first module is **Identity Validator** for authentication.

### Key Principles

- **Zero Runtime Dependency**: All authentication logic runs inside the client's application backend
- **No PII Storage**: Primus never stores or processes end-user data or tokens
- **Client-Side Integration**: Modules are integrated as packages in client applications
- **Admin Control Plane**: Primus Portal is an internal tool for managing applications and generating integration docs

---

## Repository Structure

```text
primus-saas-platform/
├── portal/                    # Primus SaaS Platform Portal (internal control plane)
│   ├── backend/              # .NET 8 Web API
│   └── frontend/             # React + TypeScript SPA
├── modules/                   # SDK Modules
│   ├── identity-validator-dotnet/   # .NET package
│   └── identity-validator-node/     # Node/TS package
├── examples/                  # Example client applications
│   ├── dotnet-api-example/
│   └── node-api-example/
├── docs/                      # Documentation
│   ├── PRD.md
│   ├── ARCHITECTURE.md
│   └── FAQ.md
└── .github/
    └── workflows/             # CI/CD pipelines
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

### 2. Identity Validator SDKs

JWT authentication packages for securing your APIs with Primus Portal integration.

#### 📦 Packages

**.NET SDK**: `PrimusSaaS.Identity.Validator` ![NuGet](https://img.shields.io/badge/v1.0.0-ready-green)
- Target Framework: .NET 7.0+
- Package Size: ~11 KB
- [Documentation](sdk/dotnet/PrimusSaaS.Identity.Validator/README.md) | [Example](examples/dotnet-api/README.md)

**Node.js SDK**: `primus-identity-validator` ![npm](https://img.shields.io/badge/v1.0.0-published-blue)
- Runtime: Node.js 16+
- Package Size: 18.9 KB
- [NPM Package](https://www.npmjs.com/package/primus-identity-validator) | [Documentation](sdk/nodejs/primus-identity-validator/README.md) | [Example](examples/nodejs-express/README.md)

#### ✨ Features

- ✅ JWT Bearer authentication
- ✅ Role-based access control (RBAC)
- ✅ User information extraction
- ✅ Automatic token validation
- ✅ TypeScript support (Node.js)
- ✅ 43 comprehensive tests (100% passing)

#### 🚀 Quick Install

```bash
# .NET
dotnet add package PrimusSaaS.Identity.Validator

# Node.js
npm install primus-identity-validator
```

For detailed integration guides, see the [SDK documentation](sdk/) and [example projects](examples/).

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
3. Install the SDK package (v1.0.0 released):

   ```bash
   # .NET
   dotnet add package PrimusSaaS.Identity.Validator --version 1.0.0
   
   # Node.js
   npm install @primus-saas/identity-validator@1.0.0
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

## Development

See individual README files in each component directory:

- [Portal Backend](./portal/backend/README.md)
- [Portal Frontend](./portal/frontend/README.md)
- [Identity Validator .NET SDK](./sdk/dotnet/PrimusSaaS.Identity.Validator/README.md)
- [Identity Validator Node.js SDK](./sdk/nodejs/primus-identity-validator/README.md)
- [.NET API Example](./examples/dotnet-api/README.md)
- [Node.js Express Example](./examples/nodejs-express/README.md)

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
