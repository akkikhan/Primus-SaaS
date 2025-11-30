---
id: feature-flags
title: Feature Flags
sidebar_position: 5
description: Percentage rollouts, user/group targeting, and time windows for .NET APIs.
---

Feature evaluation service with in-memory provider by default; supports percentage rollouts, user/group allowlists, and start/end times.

## Packages

- **.NET**: `PrimusSaaS.FeatureFlags`

## Install

```bash
dotnet add package PrimusSaaS.FeatureFlags
```

## Configuration (appsettings.json)

```json
{
  "PrimusFeatureFlags": {
    "Enabled": true,
    "Flags": {
      "NewDashboard": {
        "Description": "Roll out the new dashboard",
        "Enabled": true,
        "RolloutPercentage": 50,
        "EnabledForGroups": [ "beta-testers" ],
        "EnabledForUsers": [ "user-123" ],
        "StartTime": "2024-01-01T00:00:00Z",
        "EndTime": "2024-12-31T23:59:59Z"
      }
    }
  }
}
```

## Wiring (Program.cs)

```csharp
using PrimusSaaS.FeatureFlags;

builder.Services.AddPrimusFeatureFlags(options =>
{
    builder.Configuration.GetSection("PrimusFeatureFlags").Bind(options);
});
```

## Evaluating

```csharp
var evalContext = new FeatureFlagContext
{
    UserId = userId ?? "anonymous",
    Email = email
};

var enabled = featureFlags.IsEnabled("NewDashboard", evalContext);
```

## Demo endpoints (Live Demo)

- `GET /feature-flags/test` — returns all flags with per-user evaluation.
- `GET /feature-flags/{flagName}` — returns evaluation + definition for one flag.

Both rely on `IFeatureFlagService` from `AddPrimusFeatureFlags()` and use authenticated claims to build `FeatureFlagContext`.

## Rollout options

- Global enable/disable: `Enabled`
- Percentage rollout: `RolloutPercentage`
- User allowlist: `EnabledForUsers`
- Group allowlist: `EnabledForGroups` (supply group/role names in context)
- Time windows: `StartTime`/`EndTime` (UTC)

## Security

- Keep flag endpoints behind auth; they expose configuration.
- Use HTTPS and standard auth middleware before flag routes.
- In-memory provider is fine for dev/demo; use a persistent provider for production if required.

## See also

- Live Demo wiring + endpoints: [Live Demo API Blueprint](/docs/modules/live-demo-api)
- Versions for all modules: [Modules Version Matrix](/docs/modules/version-matrix)
