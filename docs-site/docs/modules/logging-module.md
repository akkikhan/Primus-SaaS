---
id: logging-module
title: Logging Module
sidebar_position: 2
---

Structured, enriched logging with PII masking and correlation IDs. Works with Express and ASP.NET Core, offers async buffering, rotation, and bridges to Serilog/NLog.

## Packages

- **.NET**: `PrimusSaaS.Logging` (ILogger provider + middleware)
- **Node.js**: `@primus-saas/logging` (Express middleware)

Version source of truth: [Modules Version Matrix](/docs/modules/version-matrix).
## Highlights

- Context enrichment: request/user/tenant scopes, correlation IDs, timers.
- Safety: PII masking, safe serialization, async buffering with drop metrics.
- Targets: console, file (rotation), Application Insights, Serilog/NLog bridge.
- Health & metrics: lightweight counters and health snapshots for dashboards.

## Quick Install

```bash
# Node.js / Express
npm install @primus-saas/logging

# .NET / ASP.NET Core
dotnet add package PrimusSaaS.Logging
```

## Get Started

- Follow the full walkthrough: [Client Integration Guide (Identity + Logging)](/docs/modules/client-integration-guide)
- See .NET + Node examples for middleware, DI wiring, and target configuration.
