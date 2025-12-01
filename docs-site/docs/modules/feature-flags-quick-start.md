---
id: feature-flags-quick-start
title: Feature Flags - Quick Start
sidebar_position: 40
description: 5-minute setup for feature toggles with percentage rollouts.
---

# Feature Flags Quick Start

Add feature toggles with percentage rollouts in under 5 minutes.

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
    builder.Configuration.GetSection("FeatureFlags").Bind(opts));

var app = builder.Build();
app.Run();
```

---

## Configure

### appsettings.json

```json
{
  "FeatureFlags": {
    "Flags": {
      "NewDashboard": {
        "Enabled": true,
        "RolloutPercentage": 50
      },
      "BetaFeature": {
        "Enabled": true,
        "AllowedUsers": ["user-123", "user-456"]
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

### Check Flag with User Context

```csharp
var userId = User.FindFirst("sub")?.Value;

if (_flags.IsEnabled("BetaFeature", userId))
{
    // Show beta feature
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
| User targeting | [Advanced Features →](/docs/modules/feature-flags-advanced) |
| Percentage rollouts | [Advanced Features →](/docs/modules/feature-flags-advanced) |
| External providers | [Advanced Features →](/docs/modules/feature-flags-advanced) |
| Full reference | [Feature Flags Reference →](/docs/modules/feature-flags) |
