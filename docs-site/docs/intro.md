---
id: intro
title: Primus SaaS Platform
slug: /
---

# Welcome to Primus SaaS Platform

## What is Primus SaaS Platform?

**Primus SaaS Platform** is a comprehensive developer platform that provides production-ready, modular backend components as reusable packages. Built for modern application development, Primus eliminates repetitive integration work by offering battle-tested modules that seamlessly integrate into your Node.js and .NET applications.

Instead of building authentication, logging, and other cross-cutting concerns from scratch for every project, integrate Primus modules in minutes and focus on your core business logic.

---

## Why Primus?

### Reduce Development Time

Integrate enterprise-grade authentication, logging, and other modules in minutes, not weeks. Each module comes with comprehensive documentation, working examples, and production-ready configurations.

### Zero Runtime Dependencies

All modules run entirely within your application. No external API calls to Primus services, no PII data leaves your infrastructure, and complete control over your backend logic.

### Multi-Platform Support

Full support for both Node.js (Express, NestJS) and .NET (7.0+, Minimal API, MVC) ecosystems with consistent APIs and behavior across platforms.

### Enterprise-Ready

Built with enterprise requirements in mind: comprehensive audit logging, multi-tenant support, role-based access control, and production monitoring capabilities.

---

## Available Modules

### Identity Validator

Multi-issuer JWT/OIDC token validation middleware for securing your APIs with support for Azure AD, custom JWT providers, and any OIDC-compliant identity provider.

**Key Features:**
- Multi-issuer authentication (Azure AD, Local, Custom JWT)
- Role-based access control (RBAC)
- Automatic token validation and refresh
- JWKS caching and rotation support
- Full TypeScript support

**Packages:**
- **Node.js**: `@primus-saas/identity-validator` (npm)
- **.NET**: `PrimusSaaS.Identity.Validator` (NuGet)

[Learn more about Identity Validator](modules/identity-validator-nodejs)

### Logging Module

Enterprise-grade structured logging with multiple targets and audit trail capabilities.

---

## Platform Architecture

### Client-Side Integration
Primus modules are distributed as **NPM and NuGet packages** that you integrate directly into your application codebase. All authentication, validation, and business logic runs inside your application—not on Primus servers.

### No PII Storage
Primus never stores or processes your users' personal information, tokens, or credentials. Everything stays within your infrastructure.

### Admin Control Plane
The Primus Portal is an internal management interface for configuring module settings and generating integration documentation for your development teams.

## Quick Start

### For Node.js Applications

```bash
# Install the Identity Validator
npm install @primus-saas/identity-validator
```

```typescript
import express from 'express';
import { primusIdentityMiddleware } from '@primus-saas/identity-validator';

const app = express();

// Configure multi-issuer authentication
app.use(primusIdentityMiddleware({
  issuers: [
    {
      name: 'AzureAD',
      type: 'oidc',
      issuer: 'https://login.microsoftonline.com/<TENANT_ID>/v2.0',
      audiences: ['api://your-app-id']
    },
    {
      name: 'LocalAuth',
      type: 'jwt',
      issuer: 'https://auth.yourcompany.com',
      secret: process.env.JWT_SECRET,
      audiences: ['api://your-app-id']
    }
  ]
}));

// Protected route
app.get('/api/secure', (req, res) => {
  res.json({ user: req.user });
});
```

### For .NET Applications

```bash
# Install the Identity Validator
dotnet add package PrimusSaaS.Identity.Validator
```

```csharp
using PrimusSaaS.Identity.Validator;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Identity validation
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new()
    {
        new IssuerConfig
        {
            Name = "AzureAD",
            Type = IssuerType.Oidc,
            Issuer = "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
            Audiences = new() { "api://your-app-id" }
        },
        new IssuerConfig
        {
            Name = "LocalAuth",
            Type = IssuerType.Jwt,
            Issuer = "https://auth.yourcompany.com",
            Secret = builder.Configuration["JwtSecret"],
            Audiences = new() { "api://your-app-id" }
        }
    };
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Protected endpoint
app.MapGet("/api/secure", (HttpContext ctx) => 
    Results.Ok(new { User = ctx.User.Identity?.Name }))
    .RequireAuthorization();

app.Run();
```

## System Requirements

### Node.js SDK
- **Runtime**: Node.js 16.0.0 or higher
- **Frameworks**: Express 4.x+, NestJS 8.x+
- **TypeScript**: 4.5+ (optional but recommended)
- **Dependencies**: Minimal - `jsonwebtoken`, `axios`

### .NET SDK
- **Target Framework**: .NET 7.0 or higher
- **API Styles**: Minimal API, MVC, Web API
- **Dependencies**: Microsoft.AspNetCore.Authentication.JwtBearer, System.IdentityModel.Tokens.Jwt

### No Version Conflicts
Primus modules are designed to work alongside your existing authentication infrastructure. They don't replace your identity provider—they validate tokens from any issuer you configure.

## Limitations and Constraints

### Authentication Scope
- **Token Validation Only**: Primus validates tokens issued by your configured identity providers. It does not issue tokens or provide user login UI.
- **Bring Your Own Identity Provider**: You must have an existing identity provider (Azure AD, Auth0, custom JWT issuer, etc.).

### Technical Constraints
- **Synchronous Validation**: Token validation is synchronous. For high-throughput scenarios (>10k req/sec), consider caching strategies.
- **JWKS Caching**: OIDC JWKS keys are cached for 24 hours. Key rotation is handled automatically, but immediate revocation scenarios may have a delay.

### Production Considerations
- **HTTPS Required**: In production, `RequireHttpsMetadata` must be enabled for OIDC issuers.
- **Secret Management**: JWT secrets and sensitive configuration should be stored in environment variables or secure key vaults, not in code.
- **Multi-Tenant Scenarios**: For multi-tenant applications, configure separate issuers per tenant or use dynamic issuer resolution patterns (see advanced documentation).

## Documentation Structure

### Module Guides

- **[Identity Validator - Node.js](modules/identity-validator-nodejs)**: Complete integration guide for Express and NestJS
- **[Identity Validator - .NET](modules/identity-validator-dotnet)**: Complete integration guide for .NET applications
- **[Configuration Reference](modules/identity-configuration)**: Detailed configuration options and patterns
- **[Error Reference](modules/identity-error-reference)**: Troubleshooting common errors
- **[Token Generation Guide](modules/identity-token-generation)**: How to generate test tokens for development

### Integration Patterns

- Multi-issuer authentication
- Role-based access control
- Tenant isolation strategies
- Production deployment checklist

## Getting Help

### Documentation

Comprehensive guides and API references available in the sidebar.

### Example Projects

Working example applications for both Node.js and .NET are available in the repository under `/examples`.

### Issues

Report bugs or request features on our [GitHub repository](https://github.com/akkikhan/Primus-SaaS).

## Next Steps

Choose your platform to get started:

1. **Node.js Developers**: [Identity Validator - Node.js Guide](modules/identity-validator-nodejs)
2. **.NET Developers**: [Identity Validator - .NET Guide](modules/identity-validator-dotnet)
3. **Configuration Deep Dive**: [Configuration Reference](modules/identity-configuration)

---

**Primus SaaS Platform** - Build faster. Deploy securely. Focus on what matters.
