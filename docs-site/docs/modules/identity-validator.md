---
id: identity-validator
title: Identity Validator
sidebar_position: 1
---

Enterprise-grade JWT/OIDC validation that runs entirely inside your stack. Supports multi-issuer (Azure AD + Local JWT), RBAC, and typed user context—no hosted runtime, no PII sent to Primus.

## Packages

- **.NET**: `PrimusSaaS.Identity.Validator` (ASP.NET Core middleware + helpers)
- **Node.js**: `@primus-saas/identity-validator` (Express/NestJS middleware + helpers)

Version source of truth: [Modules Version Matrix](/docs/modules/version-matrix).

## Highlights

- Multi-issuer: Azure AD (JWKS) + local JWT in the same app.
- Typed user context with claims mapping and RBAC helpers.
- Diagnostics: health checks, rate limiting for failed validations, safe serialization.
- No hosted calls: tokens validated locally; Primus never stores or sees PII.

## Quick Install

```bash
# Node.js / Express / NestJS
npm install @primus-saas/identity-validator

# .NET / ASP.NET Core
dotnet add package PrimusSaaS.Identity.Validator
```

## Get Started

- Follow the full walkthrough: [Client Integration Guide (Identity + Logging)](/docs/modules/client-integration-guide)
- See examples: Node + Express, .NET Minimal API, Azure AD mode.
