---
title: Feature Flags
description: Lightweight feature flag management for .NET and Node.js
sidebar_position: 4
---

# Feature Flags Module

Lightweight feature flag management for .NET and Node.js applications. Supports in-memory, file-based, and Azure App Configuration providers with percentage rollouts, user targeting, and time-based activation.

## Quick Links

| Resource | .NET | Node.js |
|----------|------|---------|
| **NuGet/npm** | [`PrimusSaaS.FeatureFlags`](https://www.nuget.org/packages/PrimusSaaS.FeatureFlags) | [`@primus-saas/feature-flags`](https://www.npmjs.com/package/@primus-saas/feature-flags) |
| **Version** | 1.0.0 | 1.0.0 |
| **Source** | `sdk/dotnet/PrimusSaaS.FeatureFlags` | `sdk/nodejs/primus-feature-flags` |

## Features

- **Multiple Providers**: In-memory, JSON file, or Azure App Configuration
- **Percentage Rollouts**: Gradual feature rollouts with consistent hashing
- **User Targeting**: Enable features for specific users or groups
- **Time-Based Activation**: Schedule feature availability windows
- **Hot Reload**: JSON file provider supports live updates
- **ASP.NET Core / Express Integration**: DI extensions and middleware support

## Golden Path

### .NET

#### 1. Install Package

```bash
dotnet add package PrimusSaaS.FeatureFlags
```

#### 2. Register Services (Program.cs)

```csharp
using PrimusSaaS.FeatureFlags;

builder.Services.AddPrimusFeatureFlags(options =>
{
    builder.Configuration.GetSection("PrimusFeatureFlags").Bind(options);
});
```

#### 3. Configure (appsettings.json)

```json
{
  "PrimusFeatureFlags": {
    "Provider": "InMemory",
    "DefaultValue": false,
    "Flags": {
      "NewDashboard": {
        "Enabled": true,
        "Description": "Enable the new dashboard UI"
      },
      "BetaFeature": {
        "Enabled": false,
        "RolloutPercentage": 25,
        "EnabledForGroups": ["beta-testers"]
      }
    }
  }
}
```

#### 4. Use in Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlags;

    public DashboardController(IFeatureFlagService featureFlags)
    {
        _featureFlags = featureFlags;
    }

    [HttpGet]
    public IActionResult GetDashboard()
    {
        if (_featureFlags.IsEnabled("NewDashboard", User))
        {
            return Ok(new { version = "v2", features = GetNewFeatures() });
        }
        
        return Ok(new { version = "v1", features = GetLegacyFeatures() });
    }
}
```

#### 5. Verify

```bash
# Start the API
dotnet run

# Test the endpoint
curl http://localhost:5221/feature-flags/test
```

Expected output:
```json
{
  "success": true,
  "flags": {
    "NewDashboard": {
      "globalEnabled": true,
      "enabledForUser": true,
      "description": "Enable the new dashboard UI"
    }
  },
  "summary": {
    "totalFlags": 2,
    "enabledForUser": 1
  }
}
```

### Node.js

#### 1. Install Package

```bash
npm install @primus-saas/feature-flags
```

#### 2. Configure Service

```typescript
import { FeatureFlagService } from '@primus-saas/feature-flags';

const featureFlags = new FeatureFlagService({
  flags: {
    newDashboard: {
      enabled: true,
      description: 'Enable the new dashboard UI',
    },
    betaFeature: {
      enabled: false,
      rolloutPercentage: 25,
      enabledForGroups: ['beta-testers'],
    },
  },
});
```

#### 3. Use in Express

```typescript
import express from 'express';
import { requireFeature, useFeatureFlags } from '@primus-saas/feature-flags';

const app = express();

// Add feature flags middleware
app.use(useFeatureFlags(featureFlags));

// Protect routes with feature flags
app.get('/api/v2/dashboard',
  requireFeature(featureFlags, { featureName: 'newDashboard' }),
  (req, res) => {
    res.json({ version: 'v2', data: 'New dashboard' });
  }
);

// Check flags in handlers
app.get('/api/status', (req, res) => {
  const isNewDashboard = req.featureFlags?.isEnabled('newDashboard');
  res.json({ newDashboard: isNewDashboard });
});
```

## Configuration Options

### FeatureFlagsOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Provider` | enum | `InMemory` | Storage provider (InMemory, JsonFile, AzureAppConfiguration) |
| `DefaultValue` | bool | `false` | Default when flag not found |
| `CacheDurationSeconds` | int | `30` | Cache TTL for flag values |
| `JsonFilePath` | string | - | Path to JSON config file |
| `Flags` | dict | `{}` | In-memory flag definitions |
| `Logging.LogEvaluations` | bool | `true` | Log flag evaluations |
| `Logging.IncludeUserContext` | bool | `false` | Include user ID in logs |

### FeatureFlagDefinition

| Property | Type | Description |
|----------|------|-------------|
| `Enabled` | bool | Global enabled state |
| `Description` | string | Human-readable description |
| `RolloutPercentage` | int? | Percentage of users (0-100) |
| `EnabledForUsers` | string[] | User IDs always enabled |
| `EnabledForGroups` | string[] | Group names always enabled |
| `StartTime` | DateTimeOffset? | Activation start time (UTC) |
| `EndTime` | DateTimeOffset? | Activation end time (UTC) |
| `Metadata` | dict | Custom key-value pairs |

## Evaluation Order

Feature flags are evaluated in priority order:

1. **Time-Based**: Check if current time is within StartTime/EndTime window
2. **User Targeting**: Check if user ID is in `EnabledForUsers` list
3. **Group Targeting**: Check if user belongs to any `EnabledForGroups`
4. **Percentage Rollout**: Use consistent hashing to determine if user is in rollout
5. **Global Enabled**: Fall back to the `Enabled` flag

## Providers

### In-Memory Provider (Default)

Fastest option, configured directly in code or appsettings.json. Supports `IOptionsMonitor` for hot reload when configuration changes.

```csharp
builder.Services.AddPrimusFeatureFlags(options =>
{
    options.Provider = FeatureFlagProvider.InMemory;
    options.Flags["MyFeature"] = new FeatureFlagDefinition { Enabled = true };
});
```

### JSON File Provider

Store flags in a JSON file with automatic hot-reload:

```csharp
builder.Services.AddPrimusFeatureFlags(options =>
{
    options.Provider = FeatureFlagProvider.JsonFile;
    options.JsonFilePath = "featureflags.json";
});
```

**featureflags.json:**
```json
{
  "NewDashboard": {
    "Enabled": true,
    "RolloutPercentage": 100
  },
  "BetaFeature": {
    "Enabled": false,
    "EnabledForGroups": ["beta-testers"]
  }
}
```

### Azure App Configuration (Coming Soon)

Integration with Azure App Configuration's Feature Management:

```csharp
builder.Services.AddPrimusFeatureFlags(options =>
{
    options.Provider = FeatureFlagProvider.AzureAppConfiguration;
    options.AzureAppConfigEndpoint = "https://your-config.azconfig.io";
});
```

## Environment Variables

Feature flags can be configured via environment variables:

```env
# Provider settings
PRIMUSFEATUREFLAGS__PROVIDER=InMemory
PRIMUSFEATUREFLAGS__DEFAULTVALUE=false

# Flag definitions
PRIMUSFEATUREFLAGS__FLAGS__NEWDASHBOARD__ENABLED=true
PRIMUSFEATUREFLAGS__FLAGS__NEWDASHBOARD__ROLLOUTPERCENTAGE=50
PRIMUSFEATUREFLAGS__FLAGS__BETAFEATURE__ENABLED=false
PRIMUSFEATUREFLAGS__FLAGS__BETAFEATURE__ENABLEDFORGROUPS__0=beta-testers
```

## API Reference

### IFeatureFlagService

```csharp
public interface IFeatureFlagService
{
    // Synchronous evaluation
    bool IsEnabled(string featureName);
    bool IsEnabled(string featureName, string userId);
    bool IsEnabled(string featureName, ClaimsPrincipal user);
    bool IsEnabled(string featureName, FeatureFlagContext context);
    
    // Asynchronous evaluation
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);
    Task<bool> IsEnabledAsync(string featureName, string userId, CancellationToken ct = default);
    
    // Management
    Task<IReadOnlyDictionary<string, bool>> GetAllFlagsAsync(CancellationToken ct = default);
    Task<FeatureFlagDefinition?> GetFlagDefinitionAsync(string featureName, CancellationToken ct = default);
    Task RefreshAsync(CancellationToken ct = default);
}
```

### FeatureFlagContext

```csharp
var context = new FeatureFlagContext
{
    UserId = "user123",
    Email = "user@example.com",
    Groups = new List<string> { "admins", "beta-testers" },
    Attributes = new Dictionary<string, string> { ["region"] = "us-west" }
};

var isEnabled = _featureFlags.IsEnabled("MyFeature", context);
```

## Best Practices

1. **Use meaningful flag names**: `NewCheckoutFlow` not `feature1`
2. **Set descriptions**: Document what each flag controls
3. **Clean up old flags**: Remove flags after full rollout
4. **Use percentage rollouts**: Gradual rollouts reduce risk
5. **Prefer user claims**: Use `ClaimsPrincipal` overload for authenticated users
6. **Monitor evaluations**: Enable logging to track flag usage

## Related Modules

- [Identity Validator](./identity-validator.md) - Multi-issuer JWT validation
- [Logging](./logging.md) - Structured logging with PII masking
- [Notifications](./notifications.md) - Email/SMS notifications

## Changelog

### v1.0.0
- Initial release
- In-memory and JSON file providers
- Percentage rollout with consistent hashing
- User and group targeting
- Time-based activation windows
- ASP.NET Core DI integration
- Express.js middleware
