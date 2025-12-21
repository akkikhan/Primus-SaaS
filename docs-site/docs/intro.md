---
id: intro
title: Primus SaaS Platform
slug: /intro
---

# Welcome to Primus SaaS Platform

## What is Primus SaaS Platform?
Primus is a set of production-ready backend modules you drop into your app (Node.js or .NET). They live entirely inside your codebase—no hosted runtime, no data leaves your stack—so you get security and observability fast without wiring everything from scratch.

## How it helps
- Ship faster: prebuilt auth, logging, notifications.
- Stay in control: zero external Primus services; all processing happens in your app.
- Cross-platform: consistent patterns for Node.js and .NET.
- Privacy-first: PII stays in your environment.

## Modules
- **Identity Validator** (.NET/Node): multi-issuer JWT/OIDC validation that plugs into `[Authorize]` / middleware.
- **Logging** (.NET/Node): structured logs with correlation IDs, PII masking, console/file/App Insights.
- **Notifications** (.NET): templated email/SMS with SMTP/Twilio, logger fallback, optional queue.
- **Feature Flags** (preview): percentage/user/group targeting with pluggable providers (not yet on public NuGet).
- **Document Renderer**: text-to-PDF renderer; Markdown/HTML inputs are converted to plain text.

## Get started
- Identity: `/docs/modules/identity-quick-start`
- Logging: `/docs/modules/logging-quick-start`
- Notifications: `/docs/modules/notifications-quick-start`
- Version matrix: `/docs/modules/version-matrix`
