---
id: feature-flags
title: Feature Flags
sidebar_position: 39
description: Overview and configuration reference for Primus Feature Flags.
---

# Feature Flags

Lightweight, in-process feature toggles with optional user/group targeting and percentage rollouts. All evaluation runs inside your app—no remote calls.

:::warning Publish status
`PrimusSaaS.FeatureFlags` is not yet on public NuGet. Use your internal feed or local build until the package is published.
:::

---

## Install

```bash
dotnet add package PrimusSaaS.FeatureFlags
```

---

## Minimal setup

Program.cs
```csharp
using PrimusSaaS.FeatureFlags;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusFeatureFlags(opts =>
    builder.Configuration.GetSection("PrimusFeatureFlags").Bind(opts));

builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();
app.Run();
```

appsettings.json
```json
{
  "PrimusFeatureFlags": {
    "Flags": {
      "NewDashboard": { "Enabled": true, "RolloutPercentage": 50 },
      "BetaFeature": { "Enabled": true, "EnabledForUsers": ["user-123"] },
      "MaintenanceMode": { "Enabled": false }
    }
  }
}
```

Check a flag
```csharp
public class DashboardController : ControllerBase
{
    private readonly IFeatureFlagService _flags;
    public DashboardController(IFeatureFlagService flags) => _flags = flags;

    [HttpGet]
    public IActionResult Get()
    {
        var enabled = _flags.IsEnabled("NewDashboard");
        return Ok(new { dashboard = enabled ? "new" : "legacy" });
    }
}
```

---

## Configuration reference (PrimusFeatureFlags)

| Key | Type | Required | Default | What it does |
| --- | ---- | -------- | ------- | ------------- |
| `Provider` | enum `InMemory` &#124; `JsonFile` &#124; `AzureAppConfiguration` | No | `InMemory` | Where flags are read from. |
| `JsonFilePath` | string | When `Provider=JsonFile` | — | Path to a JSON file with the same `Flags` shape. Supports reload on change. |
| `AzureAppConfigConnectionString` | string | When using connection string | — | Azure App Configuration connection string. |
| `AzureAppConfigEndpoint` | string | When using managed identity | — | Azure App Configuration endpoint (used with DefaultAzureCredential). |
| `AzureAppConfigLabel` | string | No | — | Label filter when reading flags from Azure App Configuration. |
| `CacheDurationSeconds` | int | No | 30 | Cache duration for flag values. |
| `EnableRealTimeRefresh` | bool | No | false | Enable real-time refresh (provider support required). |
| `RefreshIntervalSeconds` | int | No | 30 | Polling interval when real-time refresh is on. |
| `DefaultValue` | bool | No | false | Returned when a flag name is missing. |
| `Flags` | object | Yes for `InMemory` | — | Dictionary of feature definitions keyed by flag name. |
| `Logging:LogEvaluations` | bool | No | true | Emit log entries for evaluations. |
| `Logging:IncludeUserContext` | bool | No | false | Include user id in logs; keep off if you avoid PII in logs. |
| `Logging:LogLevel` | string | No | Information | Minimum level for evaluation logs. |

### Flag definition fields

| Field | Type | Required | Default | Description |
| ----- | ---- | -------- | ------- | ----------- |
| `Enabled` | bool | Yes | false | Global on/off switch. |
| `Description` | string | No | — | Short description of the flag. |
| `RolloutPercentage` | int 0-100 | No | — | Sticky percentage rollout based on `UserId`. |
| `EnabledForUsers` | string[] | No | `[]` | Always-on allow list of user ids. |
| `EnabledForGroups` | string[] | No | `[]` | Always-on allow list of groups. |
| `StartTime` | ISO datetime (UTC) | No | — | Activates at or after this time. |
| `EndTime` | ISO datetime (UTC) | No | — | Turns off after this time. |
| `Metadata` | object | No | `{}` | Free-form key/value notes. |

---

## Providers

- **InMemory** (default): flags come from `appsettings.json` and reload on change when supported.
- **JsonFile**: set `Provider: "JsonFile"` and `JsonFilePath: "flags.json"`; file uses the same `Flags` shape.
- **AzureAppConfiguration**: set `Provider: "AzureAppConfiguration"` and either `AzureAppConfigConnectionString` or `AzureAppConfigEndpoint` (with managed identity). Use `AzureAppConfigLabel` to filter.

:::warning Azure App Configuration
The Azure App Configuration provider currently throws `NotSupportedException` at runtime. A separate `PrimusSaaS.FeatureFlags.AzureAppConfig` package is required but not yet shipped—use `InMemory` or `JsonFile` until that package is available.
:::

---

## Optional targeting

You can pass user context when checking a flag:
```csharp
var enabled = await flags.IsEnabledAsync("BetaFeature", new FeatureFlagContext {
    UserId = "user-123",
    Groups = new() { "admins" }
});
```
User context is optional; if omitted, only the global `Enabled`/`RolloutPercentage` logic is used.

---

## Troubleshooting

- **Flag missing**: service returns `DefaultValue` (false by default). Set `DefaultValue` if you want missing flags to act as on/off consistently.
- **Rollout not sticky**: ensure you pass a stable `UserId`; percentage rollout is hashed on `feature:userId`.
- **No logs**: set `Logging:LogEvaluations` to true and adjust `Logging:LogLevel`.

---

## Next steps

- Keep secrets (Azure App Configuration connection strings) in user secrets or Key Vault; do not commit them.
- Add smoke tests around your flag-dependent endpoints so changes in configuration are caught early.
