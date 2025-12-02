---
id: feature-flags
title: Feature Flags
sidebar_position: 5
description: Percentage rollouts, user/group targeting, and time windows for .NET APIs.
---

# Feature Flags Module

:::warning Publish status
The `PrimusSaaS.FeatureFlags` package is not yet available on public NuGet. Samples below assume internal/local builds; update once the package is published.
:::

## 1. Module Overview

**PrimusSaaS.FeatureFlags** is a lightweight feature flag management system for .NET applications. It enables controlled feature rollouts through percentage-based targeting, user/group allowlists, and time-windowed activation.

Key capabilities include:
- **Multiple Providers**: In-memory (default), JSON file with hot-reload, or Azure App Configuration
- **Percentage Rollouts**: Gradual rollouts using consistent hashing per user
- **User Targeting**: Enable features for specific users or groups
- **Time-Based Activation**: Schedule feature availability with start/end windows
- **ClaimsPrincipal Integration**: Evaluate flags directly from ASP.NET Core `HttpContext.User`
- **Async & Sync APIs**: Both blocking and async evaluation methods

---

## 2. NuGet Installation

```bash
dotnet add package PrimusSaaS.FeatureFlags --version 1.0.0
```

Or add to your `.csproj`:

```xml
<PackageReference Include="PrimusSaaS.FeatureFlags" Version="1.0.0" />
```

Then restore packages:

```bash
dotnet restore
```

---

## 3. Using Statement

Add this using directive at the top of your `Program.cs` and any files that consume the feature flag service:

```csharp
using PrimusSaaS.FeatureFlags;
```

---

## 4. Program.cs Registration

### Option A: Configuration Binding (Recommended)

```csharp
using PrimusSaaS.FeatureFlags;

var builder = WebApplication.CreateBuilder(args);

// Add Feature Flags with configuration binding
builder.Services.AddPrimusFeatureFlags(options =>
{
    builder.Configuration.GetSection("PrimusFeatureFlags").Bind(options);
});

// Add controllers or other services
builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Option B: Programmatic Configuration

```csharp
using PrimusSaaS.FeatureFlags;

var builder = WebApplication.CreateBuilder(args);

// Configure flags inline
builder.Services.AddPrimusFeatureFlags(options =>
{
    options.Provider = FeatureFlagProvider.InMemory;
    options.DefaultValue = false;  // Default when flag not found
    
    options.Flags["NewDashboard"] = new FeatureFlagDefinition
    {
        Enabled = true,
        Description = "Enable the new dashboard UI",
        RolloutPercentage = 50  // 50% of users
    };
    
    options.Flags["BetaFeature"] = new FeatureFlagDefinition
    {
        Enabled = false,
        EnabledForGroups = ["beta-testers", "internal-users"],
        EnabledForUsers = ["user-123", "admin@company.com"]
    };
    
    options.Flags["HolidaySale"] = new FeatureFlagDefinition
    {
        Enabled = true,
        Description = "Holiday sale banner",
        StartTime = new DateTimeOffset(2024, 12, 20, 0, 0, 0, TimeSpan.Zero),
        EndTime = new DateTimeOffset(2025, 1, 5, 23, 59, 59, TimeSpan.Zero)
    };
});

var app = builder.Build();
app.Run();
```

### Option C: JSON File Provider

```csharp
builder.Services.AddPrimusFeatureFlags(options =>
{
    options.Provider = FeatureFlagProvider.JsonFile;
    options.JsonFilePath = "featureflags.json";  // Supports hot-reload
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

---

## 5. appsettings.json Configuration

### Full Configuration Schema

```json
{
  "PrimusFeatureFlags": {
    "Provider": "InMemory",
    "DefaultValue": false,
    "CacheDurationSeconds": 30,
    "EnableRealTimeRefresh": false,
    "RefreshIntervalSeconds": 30,
    "Flags": {
      "NewDashboard": {
        "Enabled": true,
        "Description": "Enable the redesigned dashboard",
        "RolloutPercentage": 50
      },
      "BetaFeature": {
        "Enabled": false,
        "Description": "Beta testing feature",
        "RolloutPercentage": 25,
        "EnabledForUsers": ["user-123", "user-456"],
        "EnabledForGroups": ["beta-testers", "admins"],
        "StartTime": "2024-01-01T00:00:00Z",
        "EndTime": "2024-12-31T23:59:59Z"
      },
      "PremiumFeature": {
        "Enabled": true,
        "EnabledForGroups": ["premium-subscribers"]
      }
    },
    "Logging": {
      "LogEvaluations": true,
      "IncludeUserContext": false,
      "LogLevel": "Information"
    }
  }
}
```

### Configuration Properties Reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Provider` | string | `"InMemory"` | Provider type: `InMemory`, `JsonFile`, or `AzureAppConfiguration` |
| `DefaultValue` | bool | `false` | Value returned when flag is not found |
| `CacheDurationSeconds` | int | `30` | Cache duration for flag values |
| `EnableRealTimeRefresh` | bool | `false` | Enable real-time refresh (where supported) |
| `RefreshIntervalSeconds` | int | `30` | Interval for refresh polling |
| `JsonFilePath` | string | `null` | Path to JSON file (for JsonFile provider) |
| `AzureAppConfigConnectionString` | string | `null` | Connection string for Azure App Config |
| `AzureAppConfigEndpoint` | string | `null` | Azure App Config endpoint URL |

### Flag Definition Properties

| Property | Type | Description |
|----------|------|-------------|
| `Enabled` | bool | Global enabled state |
| `Description` | string | Human-readable description |
| `RolloutPercentage` | int (0-100) | Percentage of users to enable for |
| `EnabledForUsers` | string[] | User IDs always enabled |
| `EnabledForGroups` | string[] | Group names always enabled |
| `StartTime` | DateTime (UTC) | Activation start time |
| `EndTime` | DateTime (UTC) | Activation end time |
| `Metadata` | object | Custom key-value metadata |

---

## 6. Middleware Pipeline

Feature Flags is **a service-based module** and does not require middleware registration. Inject `IFeatureFlagService` into your controllers or services.

**Recommended order (when combined with other Primus modules):**

```csharp
var app = builder.Build();

// 1. Development exception handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// 2. HTTPS redirection
app.UseHttpsRedirection();

// 3. Routing
app.UseRouting();

// 4. Authentication (required for User claims)
app.UseAuthentication();

// 5. Authorization
app.UseAuthorization();

// 6. Map endpoints
app.MapControllers();

app.Run();
```

> **Note:** Feature Flags requires authentication middleware if you're evaluating flags based on `ClaimsPrincipal` (user).

---

## 7. Dependencies

PrimusSaaS.FeatureFlags has minimal dependencies:

| Dependency | Version | Purpose |
|------------|---------|---------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 6.0+ | DI integration |
| `Microsoft.Extensions.Options` | 6.0+ | Options pattern support |
| `Microsoft.Extensions.Logging.Abstractions` | 6.0+ | Logging integration |
| `System.Text.Json` | 6.0+ | JSON parsing for file provider |

**Optional Dependencies:**
- For Azure App Configuration provider, install: `PrimusSaaS.FeatureFlags.AzureAppConfig` (coming soon)

---

## 8. External Guides

### Microsoft Documentation
- [Feature Management in ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core)
- [Azure App Configuration Feature Flags](https://learn.microsoft.com/en-us/azure/azure-app-configuration/concept-feature-management)
- [Options Pattern in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)

### Feature Flag Best Practices
- [Martin Fowler - Feature Toggles](https://martinfowler.com/articles/feature-toggles.html)
- [LaunchDarkly Feature Flag Best Practices](https://launchdarkly.com/blog/feature-flag-best-practices/)

---

## 9. End-to-End Working Example

### Complete Program.cs

```csharp
using PrimusSaaS.FeatureFlags;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// STEP 1: Configure Feature Flags
// ==============================================
builder.Services.AddPrimusFeatureFlags(options =>
{
    builder.Configuration.GetSection("PrimusFeatureFlags").Bind(options);
});

// Add authentication (required for ClaimsPrincipal evaluation)
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ==============================================
// STEP 2: Define Feature Flag Endpoints
// ==============================================

// Simple flag check
app.MapGet("/api/features/{flagName}", (
    string flagName,
    IFeatureFlagService featureFlags) =>
{
    var isEnabled = featureFlags.IsEnabled(flagName);
    return Results.Ok(new { 
        flag = flagName, 
        enabled = isEnabled 
    });
});

// User-specific flag check
app.MapGet("/api/features/{flagName}/user/{userId}", (
    string flagName,
    string userId,
    IFeatureFlagService featureFlags) =>
{
    var isEnabled = featureFlags.IsEnabled(flagName, userId);
    return Results.Ok(new { 
        flag = flagName, 
        userId = userId,
        enabled = isEnabled 
    });
});

// Check flag with custom context
app.MapPost("/api/features/{flagName}/evaluate", (
    string flagName,
    [FromBody] FeatureFlagContext context,
    IFeatureFlagService featureFlags) =>
{
    var isEnabled = featureFlags.IsEnabled(flagName, context);
    return Results.Ok(new {
        flag = flagName,
        context = new { context.UserId, context.Email, context.Groups },
        enabled = isEnabled
    });
});

// List all flags
app.MapGet("/api/features", async (IFeatureFlagService featureFlags) =>
{
    var allFlags = await featureFlags.GetAllFlagsAsync();
    return Results.Ok(allFlags);
});

// Get flag definition
app.MapGet("/api/features/{flagName}/definition", async (
    string flagName,
    IFeatureFlagService featureFlags) =>
{
    var definition = await featureFlags.GetFlagDefinitionAsync(flagName);
    if (definition == null)
        return Results.NotFound(new { error = $"Flag '{flagName}' not found" });
    
    return Results.Ok(definition);
});

app.MapControllers();
app.Run();
```

### Example Controller Using Feature Flags

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.FeatureFlags;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlags;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IFeatureFlagService featureFlags,
        ILogger<DashboardController> logger)
    {
        _featureFlags = featureFlags;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetDashboard()
    {
        // Evaluate flag with current user's claims
        if (_featureFlags.IsEnabled("NewDashboard", User))
        {
            _logger.LogInformation("Serving new dashboard for user");
            return Ok(new { 
                version = "v2.0", 
                layout = "modern",
                features = GetNewDashboardFeatures() 
            });
        }
        
        _logger.LogInformation("Serving legacy dashboard for user");
        return Ok(new { 
            version = "v1.0", 
            layout = "classic",
            features = GetLegacyFeatures() 
        });
    }

    [HttpGet("beta")]
    public async Task<IActionResult> GetBetaFeatures()
    {
        // Async evaluation
        var hasBetaAccess = await _featureFlags.IsEnabledAsync("BetaFeature", User);
        
        if (!hasBetaAccess)
        {
            return Forbid("You don't have access to beta features");
        }

        return Ok(new { 
            beta = true,
            features = new[] { "AdvancedAnalytics", "AIAssistant", "CustomThemes" }
        });
    }

    [HttpGet("holiday-sale")]
    public IActionResult GetHolidaySale()
    {
        // Time-based flag
        if (_featureFlags.IsEnabled("HolidaySale"))
        {
            return Ok(new {
                active = true,
                message = "Holiday Sale - 30% off all subscriptions!",
                discount = 30
            });
        }

        return Ok(new { active = false });
    }

    private string[] GetNewDashboardFeatures() => 
        new[] { "RealTimeCharts", "DragAndDrop", "DarkMode" };
    
    private string[] GetLegacyFeatures() => 
        new[] { "BasicCharts", "StaticLayout" };
}
```

### appsettings.json for the Example

```json
{
  "PrimusFeatureFlags": {
    "Provider": "InMemory",
    "DefaultValue": false,
    "Flags": {
      "NewDashboard": {
        "Enabled": true,
        "Description": "Redesigned dashboard with modern UI",
        "RolloutPercentage": 50
      },
      "BetaFeature": {
        "Enabled": false,
        "Description": "Early access features for beta testers",
        "EnabledForGroups": ["beta-testers", "internal"],
        "EnabledForUsers": ["developer@company.com"]
      },
      "HolidaySale": {
        "Enabled": true,
        "Description": "Holiday sale promotional banner",
        "StartTime": "2024-12-20T00:00:00Z",
        "EndTime": "2025-01-05T23:59:59Z"
      }
    },
    "Logging": {
      "LogEvaluations": true,
      "IncludeUserContext": false
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Test with cURL

```bash
# Check if flag is enabled globally
curl http://localhost:5000/api/features/NewDashboard

# Check for specific user
curl http://localhost:5000/api/features/BetaFeature/user/developer@company.com

# Evaluate with context
curl -X POST http://localhost:5000/api/features/BetaFeature/evaluate \
  -H "Content-Type: application/json" \
  -d '{"userId":"user123","email":"user@example.com","groups":["beta-testers"]}'

# List all flags
curl http://localhost:5000/api/features

# Get flag definition
curl http://localhost:5000/api/features/NewDashboard/definition
```

---

## 10. Troubleshooting Section

### Issue: Flag Always Returns False

**Symptoms:** `IsEnabled()` always returns `false` even when `Enabled = true`.

**Possible Causes & Solutions:**

1. **Flag name mismatch:**
   ```csharp
   // Case-insensitive, but check exact spelling
   _featureFlags.IsEnabled("NewDashBoard") // "NewDashboard" vs "NewDashBoard"
   ```

2. **Configuration section not bound:**
   ```csharp
   // Ensure you bind the configuration
   builder.Services.AddPrimusFeatureFlags(options =>
   {
       builder.Configuration.GetSection("PrimusFeatureFlags").Bind(options);
   });
   ```

3. **Time window hasn't started:**
   ```json
   {
     "StartTime": "2025-01-01T00:00:00Z"  // Future date
   }
   ```

4. **Percentage rollout excludes user:**
   ```json
   {
     "RolloutPercentage": 10  // Only 10% of users
   }
   ```

### Issue: User Targeting Not Working

**Symptoms:** Flag is disabled even though user is in `EnabledForUsers`.

**Debug Steps:**

```csharp
// 1. Verify user ID extraction
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
Console.WriteLine($"User ID from claims: {userId}");

// 2. Check exact match in configuration
var definition = await _featureFlags.GetFlagDefinitionAsync("MyFlag");
Console.WriteLine($"EnabledForUsers: {string.Join(", ", definition.EnabledForUsers)}");

// 3. Use explicit context
var context = new FeatureFlagContext
{
    UserId = userId,
    Email = User.FindFirst(ClaimTypes.Email)?.Value,
    Groups = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
};
var result = _featureFlags.IsEnabled("MyFlag", context);
```

### Issue: Group Targeting Not Working

**Symptoms:** Flag is disabled for users in allowed groups.

**Solution:** Ensure groups are populated in the context:

```csharp
// ClaimsPrincipal must have Role claims
var context = FeatureFlagContext.FromClaimsPrincipal(User);

// Or manually specify groups
var context = new FeatureFlagContext
{
    UserId = "user123",
    Groups = ["admins", "beta-testers"]  // List of group names
};
```

### Issue: JSON File Provider Not Updating

**Symptoms:** Changes to `featureflags.json` don't take effect.

**Solutions:**

1. **Check file path:**
   ```csharp
   options.JsonFilePath = Path.Combine(AppContext.BaseDirectory, "featureflags.json");
   ```

2. **Force refresh:**
   ```csharp
   await _featureFlags.RefreshAsync();
   ```

3. **Ensure file is valid JSON:**
   ```bash
   # Validate JSON syntax
   Get-Content featureflags.json | ConvertFrom-Json
   ```

### Enable Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "PrimusSaaS.FeatureFlags": "Debug"
    }
  },
  "PrimusFeatureFlags": {
    "Logging": {
      "LogEvaluations": true,
      "IncludeUserContext": true,
      "LogLevel": "Debug"
    }
  }
}
```

---

## 11. FAQ

### Q: Can I change flags at runtime?

**A:** With the InMemory provider, changes require app restart. Use the **JsonFile provider** with hot-reload or **Azure App Configuration** for runtime updates.

### Q: How does percentage rollout work?

**A:** The SDK uses consistent hashing on `userId + flagName` to determine if a user falls within the rollout percentage. This ensures the same user always gets the same result for a given flag (until percentage changes).

### Q: Can I use multiple providers?

**A:** Only one provider is active at a time. Configure via `options.Provider`. For hybrid scenarios, create multiple `IFeatureFlagService` instances with different providers (advanced).

### Q: How do I evaluate flags for anonymous users?

**A:** Use a session ID or generate a consistent identifier:

```csharp
var sessionId = HttpContext.Session.Id ?? Guid.NewGuid().ToString();
var enabled = _featureFlags.IsEnabled("MyFlag", sessionId);
```

### Q: What happens if a flag doesn't exist?

**A:** Returns `options.DefaultValue` (default: `false`). Set to `true` if you want unknown flags enabled by default.

### Q: How do I clean up old feature flags?

**A:** Feature flags should be removed after full rollout:
1. Set `RolloutPercentage` to 100 and monitor
2. Remove flag checks from code
3. Remove flag from configuration
4. Deploy

### Q: Can I use feature flags in background services?

**A:** Yes, inject `IFeatureFlagService`:

```csharp
public class BackgroundWorker : BackgroundService
{
    private readonly IFeatureFlagService _flags;
    
    public BackgroundWorker(IFeatureFlagService flags) => _flags = flags;
    
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_flags.IsEnabled("NewProcessingAlgorithm"))
            {
                // Use new algorithm
            }
            await Task.Delay(1000, token);
        }
    }
}
```

### Q: Is there a UI to manage flags?

**A:** The InMemory and JsonFile providers require configuration file edits. For a management UI, consider Azure App Configuration which has a portal interface.

---

## 12. Version Compatibility

| PrimusSaaS.FeatureFlags | .NET 6 | .NET 7 | .NET 8 | .NET 9 |
|------------------------|--------|--------|--------|--------|
| 1.0.0                  | ✅     | ✅     | ✅     | ❌     |

### Dependencies Matrix

| Feature Flags Version | Microsoft.Extensions.* | Min ASP.NET Core |
|-----------------------|------------------------|------------------|
| 1.0.0                 | 6.0.0+                 | 6.0              |

---

## 13. Next Module Suggestions

After configuring Feature Flags, consider adding:

| Module | Purpose | Link |
|--------|---------|------|
| **Identity Validator** | Secure your APIs with multi-issuer JWT validation | [Identity Validator →](/docs/modules/identity-validator) |
| **Logging** | Add structured logging with PII masking | [Logging Module →](/docs/modules/logging-module) |
| **Notifications** | Send emails/SMS for flag-triggered events | [Notifications →](/docs/modules/notifications) |

### Integration Example: Feature Flags + Identity

```csharp
// Combine authentication with feature flags
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

builder.Services.AddPrimusFeatureFlags(options =>
{
    builder.Configuration.GetSection("PrimusFeatureFlags").Bind(options);
});

// Use together in controllers
[Authorize]
public IActionResult GetPremiumFeature()
{
    // User is authenticated, check if they have feature access
    if (_featureFlags.IsEnabled("PremiumFeature", User))
    {
        return Ok(new { access = "granted" });
    }
    return Forbid();
}
```

---

## Further Reading

- [Live Demo Integration](/docs/modules/live-demo-api) — See Feature Flags in action
- [Version Matrix](/docs/modules/version-matrix) — All Primus module versions
