# PrimusSaaS Feature Flags

Lightweight feature flag management for .NET applications. Supports in-memory, file-based, and Azure App Configuration providers.

[![NuGet Version](https://img.shields.io/nuget/v/PrimusSaaS.FeatureFlags.svg)](https://www.nuget.org/packages/PrimusSaaS.FeatureFlags/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- **Multiple Providers**: In-memory, JSON file, or Azure App Configuration
- **Percentage Rollouts**: Gradual feature rollouts with consistent hashing
- **User Targeting**: Enable features for specific users or groups
- **Time-Based Activation**: Schedule feature availability windows
- **Hot Reload**: JSON file provider supports live updates
- **ASP.NET Core Integration**: DI extensions and middleware support

## Installation

```bash
dotnet add package PrimusSaaS.FeatureFlags
```

## Quick Start

### 1. Register Services

```csharp
// In Program.cs
builder.Services.AddPrimusFeatureFlags(options =>
{
    options.Provider = FeatureFlagProvider.InMemory;
    options.Flags["NewDashboard"] = new FeatureFlagDefinition
    {
        Enabled = true,
        Description = "Enable the new dashboard UI"
    };
    options.Flags["BetaFeature"] = new FeatureFlagDefinition
    {
        Enabled = false,
        RolloutPercentage = 25, // 25% of users
        EnabledForGroups = ["beta-testers"]
    };
});
```

### 2. Use in Controllers/Services

```csharp
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

## Configuration Options

### In-Memory Provider (Default)

```csharp
builder.Services.AddPrimusFeatureFlags(options =>
{
    options.Provider = FeatureFlagProvider.InMemory;
    options.DefaultValue = false;
    options.Flags["MyFeature"] = new FeatureFlagDefinition
    {
        Enabled = true,
        Description = "My awesome feature",
        RolloutPercentage = 50,
        EnabledForUsers = ["user1@example.com", "user2@example.com"],
        EnabledForGroups = ["admins", "beta-testers"],
        StartTime = DateTimeOffset.UtcNow,
        EndTime = DateTimeOffset.UtcNow.AddDays(30)
    };
});
```

### JSON File Provider

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
    "Description": "New dashboard UI",
    "RolloutPercentage": 100
  },
  "BetaFeature": {
    "Enabled": false,
    "RolloutPercentage": 25,
    "EnabledForGroups": ["beta-testers"]
  }
}
```

### Configuration Binding

```csharp
builder.Services.AddPrimusFeatureFlags(options =>
{
    builder.Configuration.GetSection("PrimusFeatureFlags").Bind(options);
});
```

**appsettings.json:**
```json
{
  "PrimusFeatureFlags": {
    "Provider": "InMemory",
    "DefaultValue": false,
    "CacheDurationSeconds": 30,
    "Flags": {
      "NewDashboard": {
        "Enabled": true,
        "RolloutPercentage": 50
      }
    },
    "Logging": {
      "LogEvaluations": true,
      "IncludeUserContext": false
    }
  }
}
```

## Feature Flag Evaluation

### Simple Check

```csharp
if (_featureFlags.IsEnabled("MyFeature"))
{
    // Feature is enabled globally
}
```

### User-Specific Check

```csharp
// With user ID
if (_featureFlags.IsEnabled("MyFeature", "user123"))
{
    // Feature is enabled for this user
}

// With ClaimsPrincipal (from HttpContext.User)
if (_featureFlags.IsEnabled("MyFeature", User))
{
    // Feature is enabled based on user claims
}
```

### Custom Context

```csharp
var context = new FeatureFlagContext
{
    UserId = "user123",
    Email = "user@example.com",
    Groups = ["admins", "beta-testers"],
    Attributes = { ["region"] = "us-west" }
};

if (_featureFlags.IsEnabled("MyFeature", context))
{
    // Feature is enabled for this context
}
```

### Async Evaluation

```csharp
var isEnabled = await _featureFlags.IsEnabledAsync("MyFeature", User);
```

## Targeting Rules

Feature flags are evaluated in the following order:

1. **Time-Based**: Check if current time is within StartTime/EndTime window
2. **User Targeting**: Check if user ID is in EnabledForUsers list
3. **Group Targeting**: Check if user belongs to any EnabledForGroups
4. **Percentage Rollout**: Use consistent hashing to determine if user is in rollout
5. **Global Enabled**: Fall back to the Enabled flag

## API Reference

### IFeatureFlagService

| Method | Description |
|--------|-------------|
| `IsEnabled(featureName)` | Check if feature is globally enabled |
| `IsEnabled(featureName, userId)` | Check if feature is enabled for user ID |
| `IsEnabled(featureName, ClaimsPrincipal)` | Check if feature is enabled for claims |
| `IsEnabled(featureName, context)` | Check with custom context |
| `GetAllFlagsAsync()` | Get all feature flags and states |
| `GetFlagDefinitionAsync(featureName)` | Get feature flag definition |
| `RefreshAsync()` | Force refresh from provider |

### FeatureFlagDefinition

| Property | Type | Description |
|----------|------|-------------|
| `Enabled` | bool | Global enabled state |
| `Description` | string? | Human-readable description |
| `RolloutPercentage` | int? | Percentage of users (0-100) |
| `EnabledForUsers` | List<string> | User IDs always enabled |
| `EnabledForGroups` | List<string> | Group names always enabled |
| `StartTime` | DateTimeOffset? | Activation start (UTC) |
| `EndTime` | DateTimeOffset? | Activation end (UTC) |
| `Metadata` | Dictionary<string, string> | Custom key-value pairs |

## Best Practices

1. **Use meaningful flag names**: `NewCheckoutFlow` not `feature1`
2. **Set descriptions**: Document what each flag controls
3. **Clean up old flags**: Remove flags after full rollout
4. **Use percentage rollouts**: Gradual rollouts reduce risk
5. **Monitor evaluations**: Enable logging to track usage

## Related Packages

- `PrimusSaaS.Identity.Validator` - Multi-issuer JWT validation
- `PrimusSaaS.Logging` - Structured logging with PII masking
- `PrimusSaaS.Notifications` - Email/SMS notifications

## License

MIT License - see [LICENSE](LICENSE) for details.
