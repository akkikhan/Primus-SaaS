# Primus SaaS Platform

**Version**: 1.1 MVP  
**Type**: Developer Platform for Reusable Backend Modules

📚 **[View Documentation](QUICK_START.md)** | 🚀 **[GitHub Pages Setup](GITHUB_PAGES_SETUP.md)**

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

### 2. Identity Validator Module

First horizontal module with two auth providers:

- **Local**: Username/password with local JWT issuance
- **Azure AD**: Microsoft token validation

**Packages**:

- `Primus.SaaS.IdentityValidator` (NuGet)
- `@primus-saas/identity-validator` (NPM)

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
2. Note your assigned `PrimusClientId`
3. Install the module package:

   ```bash
   # .NET
   dotnet add package Primus.SaaS.IdentityValidator --version 1.0.0
   
   # Node
   npm install @primus-saas/identity-validator@1.0.0
   ```

4. Configure authentication mode (Local, AzureAd, or Hybrid) using provided config snippets
5. Wire up middleware in your backend using provided code snippets

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
- [Identity Validator .NET](./modules/identity-validator-dotnet/README.md)
- [Identity Validator Node](./modules/identity-validator-node/README.md)

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
