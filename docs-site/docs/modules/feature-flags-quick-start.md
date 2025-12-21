---
id: feature-flags-quick-start
title: Feature Flags - Quick Start
sidebar_position: 40
description: 5-minute setup for feature toggles with percentage rollouts.
---

# Feature Flags Quick Start

:::warning Publish status
The `PrimusSaaS.FeatureFlags` package is not yet available on public NuGet. Samples below assume internal/local builds; update once the package is published.
:::

Add feature toggles with percentage rollouts in under 5 minutes.

:::info Complete Data Isolation
Primus Feature Flags runs **entirely within your application**. All flag evaluation happens locally using your configuration. No feature flag data or user context is ever transmitted to Primus servers.
:::

---

## Install

```bash
dotnet add package PrimusSaaS.FeatureFlags
```

---

## Setup (3 Lines)

```csharp
using PrimusSaaS.FeatureFlags;

var builder = WebApplication.CreateBuilder(args);

// Add this ONE line
builder.Services.AddPrimusFeatureFlags(opts => 
    builder.Configuration.GetSection("PrimusFeatureFlags").Bind(opts));

var app = builder.Build();
app.Run();
```

---

## Configure

### appsettings.json

```json
{
  "PrimusFeatureFlags": {
    "Flags": {
      "NewDashboard": {
        "Enabled": true,
        "RolloutPercentage": 50
      },
      "BetaFeature": {
        "Enabled": true,
        "EnabledForUsers": ["user-123", "user-456"]
      },
      "MaintenanceMode": {
        "Enabled": false
      }
    }
  }
}
```

---

## Use

### Check Flag in Controller

```csharp
public class DashboardController : ControllerBase
{
    private readonly IFeatureFlagService _flags;

    public DashboardController(IFeatureFlagService flags)
    {
        _flags = flags;
    }

    [HttpGet]
    public IActionResult GetDashboard()
    {
        if (_flags.IsEnabled("NewDashboard"))
        {
            return Ok(GetNewDashboardData());
        }
        
        return Ok(GetLegacyDashboardData());
    }
}
```

---

## Minimal API

```csharp
app.MapGet("/feature/{name}", (string name, IFeatureFlagService flags) =>
{
    return new { 
        feature = name, 
        enabled = flags.IsEnabled(name) 
    };
});
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Learn all options | [Feature Flags Reference ->](/docs/modules/feature-flags) |
| Use Json/Azure App Config | [Feature Flags Reference ->](/docs/modules/feature-flags#providers) |
| Troubleshoot | [Feature Flags Reference ->](/docs/modules/feature-flags#troubleshooting) |

