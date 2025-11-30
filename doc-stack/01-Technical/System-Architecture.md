# 🏛️ Primus SaaS Platform — System Architecture

**Version:** 1.0  
**Status:** 🟢 Approved  
**Audience:** Architects, Lead Developers, CTOs

---

## 1. 🎯 Executive Overview

The **Primus SaaS Platform** is a modular, client-owned SDK ecosystem designed to accelerate SaaS development. Unlike traditional SaaS platforms that force a hosted runtime, Primus operates entirely within the customer's environment.

### 🔑 Key Architectural Principles

*   **Client-Owned Runtime:** No Primus-hosted backend. The code runs 100% in your infrastructure (Azure, AWS, On-Prem).
*   **Zero PII Storage:** Primus does not store, process, or see your tenant data.
*   **Modular Design:** Features (Identity, Logging, Notifications) are decoupled SDKs. Use what you need.
*   **Environment-First:** All configuration is driven by environment variables for seamless DevOps integration.
*   **Golden Path Strategy:** Every feature ships with a canonical "Golden Path" implementation to guarantee speed-to-value.

---

## 2. 🧩 High-Level Components

The platform consists of three primary layers:

| Layer | Component | Description |
| :--- | :--- | :--- |
| **SDK Layer** | **.NET / Node.js SDKs** | The core logic libraries distributed via NuGet and npm. These contain the business logic, DI extensions, and typed options. |
| **Control Plane** | **Portal** | An internal admin tool for managing module metadata, generating docs, and controlling visibility. *Note: This is for Platform Admins, not end-users.* |
| **Experience** | **Live UI Demo** | A production-grade reference application that serves as the "Source of Truth" for how modules function end-to-end. |

### 🏗️ Component Diagram

```mermaid
graph TD
    subgraph "Customer Infrastructure"
        App[Customer App]
        SDK_Net[.NET SDK (NuGet)]
        SDK_Node[Node SDK (npm)]
        
        App --> SDK_Net
        App --> SDK_Node
    end

    subgraph "Primus Ecosystem"
        Portal[Portal (Control Plane)]
        Docs[Docs Site]
        LiveDemo[Live UI Demo]
    end

    Portal -->|Generates| Docs
    Portal -->|Defines| LiveDemo
    LiveDemo -->|Validates| SDK_Net
```

---

## 3. 🔌 Integration Model

Primus uses a **Dependency Injection (DI) + Configuration Binding** pattern. This ensures that integrating a module is as simple as adding a few lines to `Program.cs`.

### .NET Integration Pattern

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Identity Module
builder.Services.AddPrimusIdentity(options => 
    builder.Configuration.GetSection("PrimusIdentity").Bind(options));

// 2. Logging Module
builder.Logging.AddPrimus(options => 
    builder.Configuration.GetSection("PrimusLogging").Bind(options));

// 3. Notifications Module
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(opts => { /* Bind Config */ })
    .UseFileTemplates("NotificationTemplates"));
```

---

## 4. 🛡️ Security Architecture

### Identity & Authentication
*   **Multi-Issuer Support:** Validates tokens from Azure AD, Auth0, and Local JWTs simultaneously.
*   **Validation Logic:** Enforces `iss` (Issuer), `aud` (Audience), Signature, and Expiry (`exp`).
*   **No Secrets in Code:** All secrets must be injected via Environment Variables or Key Vaults.

### Data Privacy
*   **PII Masking:** The Logging module automatically masks sensitive fields (Credit Cards, SSNs) before they leave the application.
*   **No External Egress:** Primus SDKs do not "phone home" with business data.

---

## 5. 🚀 Scalability & Performance

*   **Stateless:** SDKs are designed to be stateless, allowing the host application to scale horizontally (Kubernetes/Serverless).
*   **Async-First:** All I/O operations (sending emails, validating remote tokens) are asynchronous to prevent thread blocking.
*   **Lightweight:** Modules are granular. Importing `Primus.Identity` does not drag in `Primus.Notifications`.

---

## 6. 📂 Directory Structure Standard

To maintain consistency, all Primus-integrated projects should follow this structure:

```text
/
├── sdk/                  # Core Libraries
├── portal/               # Admin Control Plane
├── examples/             # Golden Paths & Live Demos
│   ├── LiveDemoApi/      # Reference Backend
│   └── LiveDemoFrontend/ # Reference UI
├── docs-site/            # Documentation
└── NotificationTemplates/# Liquid Templates for Emails/SMS
```
