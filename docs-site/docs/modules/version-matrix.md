---
id: version-matrix
title: Modules Version Matrix
sidebar_position: 7
description: Single source of truth for module versions across NuGet and NPM.
---

Keep this table in sync with package READMEs and changelogs. Update here first, then bump NuGet/NPM packages and release notes.

| Module             | Runtime | Package                           | Current Version | Notes                                          |
| ------------------ | ------- | --------------------------------- | --------------- | ---------------------------------------------- |
| Identity Validator | .NET    | `PrimusSaaS.Identity.Validator`   | 1.5.0           | ASP.NET Core auth handler + diagnostics        |
| Identity Validator | Node.js | `@primus-saas/identity-validator` | 1.3.3           | Express/NestJS middleware + headless validator |
| Logging            | .NET    | `PrimusSaaS.Logging`              | 1.2.4           | ILogger provider + middleware                  |
| Logging            | Node.js | `@primus-saas/logging`            | 1.2.4           | Express middleware + logger                    |
| Notifications      | .NET    | `PrimusSaaS.Notifications`        | 1.4.2           | Email/SMS/templates/queues                     |
| Feature Flags      | .NET    | `PrimusSaaS.FeatureFlags`         | 1.0.0           | In-memory provider with rollout rules          |
| Document Renderer  | .NET    | `PrimusSaaS.Documents`            | 1.0.0           | Text input; Markdown/HTML rendered as plain text |

> When bumping any package, update this matrix, the package README/changelog, and any install commands in other docs (e.g., Live Demo API, Client Integration Guide).
