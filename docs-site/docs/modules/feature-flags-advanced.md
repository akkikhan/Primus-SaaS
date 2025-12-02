---
id: feature-flags-advanced
title: Feature Flags - Advanced Features
sidebar_position: 41
description: User targeting, A/B testing, external providers, and gradual rollouts.
---

# Feature Flags Advanced Features

:::warning Publish status
The `PrimusSaaS.FeatureFlags` package is not yet available on public NuGet. Samples below assume internal/local builds; update once the package is published.
:::

Unlock user targeting, A/B testing, external provider integration, and gradual rollout strategies.

---

## Percentage Rollouts

### Configure Gradual Rollout

```json
{
  "FeatureFlags": {
    "Flags": {
      "NewCheckoutFlow": {
        "Enabled": true,
        "RolloutPercentage": 25,
        "RolloutStrategy": "UserIdHash"
      }
    }
  }
}
```

### Rollout Strategies

| Strategy | Description |
|----------|-------------|
| `UserIdHash` | Consistent bucketing by user ID hash |
| `Random` | Random assignment per request |
| `SessionId` | Consistent per session |
| `OrganizationId` | All users in org get same experience |

### Programmatic Percentage Check

```csharp
var userId = User.FindFirst("sub")?.Value;

// Same user always gets same result
if (_flags.IsEnabled("NewCheckoutFlow", userId))
{
    return NewCheckoutFlow();
}
return LegacyCheckoutFlow();
```

---

## User Targeting

### Allow Specific Users

```json
{
  "FeatureFlags": {
    "Flags": {
      "BetaFeature": {
        "Enabled": true,
        "AllowedUsers": ["user-123", "user-456", "user-789"]
      }
    }
  }
}
```

### Allow User Groups

```json
{
  "FeatureFlags": {
    "Flags": {
      "AdminDashboard": {
        "Enabled": true,
        "AllowedGroups": ["admins", "power-users"]
      }
    }
  }
}
```

### Check with User Context

```csharp
public class FeatureController : ControllerBase
{
    private readonly IFeatureFlagService _flags;

    [HttpGet("check/{feature}")]
    public IActionResult CheckFeature(string feature)
    {
        var context = new FeatureContext
        {
            UserId = User.FindFirst("sub")?.Value,
            Groups = User.FindAll("groups").Select(c => c.Value).ToList(),
            Email = User.FindFirst("email")?.Value
        };
        
        return Ok(new {
            feature,
            enabled = _flags.IsEnabled(feature, context)
        });
    }
}
```

---

## Environment-Based Flags

### Configure Per Environment

```json
// appsettings.Development.json
{
  "FeatureFlags": {
    "Flags": {
      "DebugMode": { "Enabled": true },
      "NewPaymentSystem": { "Enabled": true, "RolloutPercentage": 100 }
    }
  }
}

// appsettings.Production.json
{
  "FeatureFlags": {
    "Flags": {
      "DebugMode": { "Enabled": false },
      "NewPaymentSystem": { "Enabled": true, "RolloutPercentage": 10 }
    }
  }
}
```

---

## A/B Testing

### Configure A/B Test

```json
{
  "FeatureFlags": {
    "Flags": {
      "CheckoutButtonColor": {
        "Enabled": true,
        "Variants": {
          "control": { "percentage": 50, "value": "blue" },
          "variantA": { "percentage": 25, "value": "green" },
          "variantB": { "percentage": 25, "value": "orange" }
        }
      }
    }
  }
}
```

### Get Variant

```csharp
var userId = User.FindFirst("sub")?.Value;
var variant = _flags.GetVariant("CheckoutButtonColor", userId);

return Ok(new {
    buttonColor = variant.Value,  // "blue", "green", or "orange"
    variantName = variant.Name    // "control", "variantA", or "variantB"
});
```

### Track Conversion

```csharp
// When user completes action
await _flags.TrackConversionAsync("CheckoutButtonColor", userId, new {
    orderId = order.Id,
    amount = order.Total
});
```

---

## External Providers

### LaunchDarkly Integration

```csharp
builder.Services.AddPrimusFeatureFlags(opts =>
{
    opts.Provider = FeatureFlagProvider.LaunchDarkly;
    opts.LaunchDarkly = new LaunchDarklyOptions
    {
        SdkKey = "sdk-xxx"
    };
});
```

### Azure App Configuration

```csharp
builder.Services.AddPrimusFeatureFlags(opts =>
{
    opts.Provider = FeatureFlagProvider.AzureAppConfig;
    opts.AzureAppConfig = new AzureAppConfigOptions
    {
        ConnectionString = "Endpoint=...",
        CacheExpiration = TimeSpan.FromMinutes(5)
    };
});
```

### ConfigCat

```csharp
builder.Services.AddPrimusFeatureFlags(opts =>
{
    opts.Provider = FeatureFlagProvider.ConfigCat;
    opts.ConfigCat = new ConfigCatOptions
    {
        SdkKey = "xxx"
    };
});
```

---

## Conditional Features in Razor/Blazor

### Tag Helper

```html
<feature name="NewNavigation">
    <!-- New navigation component -->
    <NewNav />
</feature>

<feature name="NewNavigation" negate="true">
    <!-- Old navigation -->
    <OldNav />
</feature>
```

### Blazor Component

```razor
@inject IFeatureFlagService Flags

@if (Flags.IsEnabled("NewDashboard"))
{
    <NewDashboard />
}
else
{
    <LegacyDashboard />
}
```

---

## Feature Flag Attribute

### Protect Endpoints

```csharp
[FeatureFlag("BetaApi")]
[HttpGet("beta/data")]
public IActionResult GetBetaData()
{
    return Ok(betaData);
}
```

### Protect Controllers

```csharp
[FeatureFlag("AdminModule")]
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    // All endpoints require AdminModule flag
}
```

---

## Dynamic Flag Updates

### Real-time Updates

```csharp
builder.Services.AddPrimusFeatureFlags(opts =>
{
    builder.Configuration.GetSection("FeatureFlags").Bind(opts);
    
    // Enable hot reload
    opts.EnableHotReload = true;
    opts.ReloadInterval = TimeSpan.FromSeconds(30);
});
```

### Change Notifications

```csharp
builder.Services.AddPrimusFeatureFlags(opts =>
{
    builder.Configuration.GetSection("FeatureFlags").Bind(opts);
    
    opts.OnFlagChanged = (flagName, newValue) =>
    {
        Console.WriteLine($"Flag {flagName} changed to {newValue}");
    };
});
```

---

## Feature Flag API

### Admin Endpoints

```csharp
app.MapGet("/admin/flags", [Authorize(Roles = "admin")] (IFeatureFlagService flags) =>
{
    return flags.GetAllFlags();
});

app.MapPut("/admin/flags/{name}", [Authorize(Roles = "admin")] async (
    string name, 
    FlagUpdate update,
    IFeatureFlagService flags) =>
{
    await flags.UpdateFlagAsync(name, update);
    return Results.Ok();
});

record FlagUpdate(bool? Enabled, int? RolloutPercentage, List<string>? AllowedUsers);
```

---

## Audit Trail

### Track Flag Access

```csharp
builder.Services.AddPrimusFeatureFlags(opts =>
{
    builder.Configuration.GetSection("FeatureFlags").Bind(opts);
    
    opts.EnableAuditLogging = true;
    opts.OnFlagEvaluated = (flagName, context, result) =>
    {
        _logger.LogInformation(
            "Flag {FlagName} evaluated for user {UserId}: {Result}",
            flagName, context?.UserId, result);
    };
});
```

---

## Testing

### Override Flags in Tests

```csharp
public class FeatureTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FeatureTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Override flag service with test implementation
                services.AddSingleton<IFeatureFlagService>(
                    new TestFeatureFlagService(new Dictionary<string, bool>
                    {
                        ["NewFeature"] = true,
                        ["BetaFeature"] = false
                    }));
            });
        });
    }

    [Fact]
    public async Task NewFeature_WhenEnabled_ReturnsNewResponse()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/dashboard");
        // Assert new behavior
    }
}
```

### Test Flag Combinations

```csharp
[Theory]
[InlineData(true, true, "new-both")]
[InlineData(true, false, "new-a-only")]
[InlineData(false, true, "new-b-only")]
[InlineData(false, false, "legacy")]
public async Task Dashboard_ReturnsCorrectVersion(
    bool flagA, 
    bool flagB, 
    string expectedVersion)
{
    // Configure flags and test
}
```

---

## Complete Example

```csharp
using PrimusSaaS.FeatureFlags;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusFeatureFlags(opts =>
{
    builder.Configuration.GetSection("FeatureFlags").Bind(opts);
    
    // Hot reload
    opts.EnableHotReload = true;
    opts.ReloadInterval = TimeSpan.FromMinutes(1);
    
    // Audit logging
    opts.EnableAuditLogging = true;
    opts.OnFlagEvaluated = (flag, ctx, result) =>
    {
        Console.WriteLine($"[FLAG] {flag} = {result} for {ctx?.UserId}");
    };
});

var app = builder.Build();

// Get all flags
app.MapGet("/flags", (IFeatureFlagService flags) => flags.GetAllFlags());

// Check specific flag
app.MapGet("/flags/{name}", (string name, IFeatureFlagService flags, HttpContext ctx) =>
{
    var userId = ctx.User.FindFirst("sub")?.Value;
    var context = new FeatureContext { UserId = userId };
    
    return new {
        flag = name,
        enabled = flags.IsEnabled(name, context)
    };
});

// Get variant for A/B test
app.MapGet("/flags/{name}/variant", (string name, IFeatureFlagService flags, HttpContext ctx) =>
{
    var userId = ctx.User.FindFirst("sub")?.Value;
    var variant = flags.GetVariant(name, userId);
    
    return new {
        flag = name,
        variant = variant.Name,
        value = variant.Value
    };
});

// Feature-gated endpoint
app.MapGet("/beta/dashboard", [FeatureFlag("BetaDashboard")] () =>
{
    return new { message = "Beta dashboard data" };
});

app.Run();
```

### appsettings.json

```json
{
  "FeatureFlags": {
    "Flags": {
      "BetaDashboard": {
        "Enabled": true,
        "RolloutPercentage": 20,
        "AllowedUsers": ["beta-tester-1"]
      },
      "NewCheckout": {
        "Enabled": true,
        "Variants": {
          "control": { "percentage": 50, "value": "original" },
          "variant": { "percentage": 50, "value": "simplified" }
        }
      },
      "MaintenanceMode": {
        "Enabled": false
      }
    }
  }
}
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Basic setup | [Quick Start →](/docs/modules/feature-flags-quick-start) |
| Full reference | [Feature Flags Reference →](/docs/modules/feature-flags) |
