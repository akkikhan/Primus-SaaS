---
id: logging-module
title: Logging Module
sidebar_position: 2
description: Enterprise-grade structured logging for .NET with PII masking, correlation IDs, and multiple output targets.
---

# Logging Module

## 1. Module Overview

The **Primus Logging Module** is an enterprise-grade structured logging library that provides automatic context enrichment, PII masking, async buffering, and multiple output targets. It seamlessly integrates with ASP.NET Core's `ILogger<T>` interface while adding powerful features like correlation IDs, performance tracking, and automatic HTTP context enrichment.

**Key benefits:**
- **Drop-in replacement**: Works with standard `ILogger<T>` interface—no code changes needed
- **PII protection**: Automatic masking of emails, credit cards, SSNs, and custom sensitive fields
- **Multiple targets**: Console, file (with rotation), Azure Application Insights, Serilog, and NLog bridges
- **Context enrichment**: Automatic request IDs, correlation IDs, user context, and tenant context
- **High performance**: Async buffering with non-blocking writes and minimal contention

---

## 2. Installation

### NuGet Package

```bash
dotnet add package PrimusSaaS.Logging
```

**Current Version**: `1.2.4` (supports .NET 6 and 7; net8 when built with net8 SDK)

See [Modules Version Matrix](/docs/modules/version-matrix) for the authoritative version list.

### Starting from scratch
- New project: `dotnet new webapi -n MyPrimusLogging --no-https`
- Add package: `cd MyPrimusLogging && dotnet add package PrimusSaaS.Logging`
- Swagger (if missing): `dotnet add package Swashbuckle.AspNetCore`
- Run: `dotnet run --urls=http://localhost:5002`

---

## 3. Required Using Statements

Add these using statements to your `Program.cs` or relevant files:

```csharp
// Core logging extensions
using PrimusSaaS.Logging.Extensions;

// For direct logger usage (optional)
using PrimusSaaS.Logging.Core;

// Alias to avoid conflict with Microsoft.Extensions.Logging.LogLevel
using PrimusLogLevel = PrimusSaaS.Logging.Core.LogLevel;
```

---

## 4. Program.cs Service Registration
Register Primus Logging and middleware so structured logs/correlation/PII masking are active.

### Quick Start (Recommended; single pattern)

Use this approach for production applications with appsettings.json:

```csharp
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Primus Logging
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(builder.Configuration.GetSection("PrimusLogging"));

var app = builder.Build();

// Add middleware for automatic HTTP context enrichment
app.UsePrimusLogging();

app.MapControllers();
app.Run();
```

### Option B: Code Configuration

Use this for simpler scenarios or when you need programmatic control:

```csharp
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.Logging.Core;
using PrimusLogLevel = PrimusSaaS.Logging.Core.LogLevel;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "my-api";
    options.Environment = builder.Environment.EnvironmentName;
    options.MinLevel = PrimusLogLevel.Info;
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() { Type = "file", Path = "logs/app.log", Async = true }
    };
});

var app = builder.Build();

app.UsePrimusLogging();
app.MapControllers();
app.Run();
```

### Option C: With Application Insights

```csharp
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(opts => 
    builder.Configuration.GetSection("PrimusLogging").Bind(opts));

// Add Application Insights telemetry
var aiConnectionString = builder.Configuration["PrimusLogging:ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(o => 
        o.ConnectionString = aiConnectionString);
}

var app = builder.Build();

app.UsePrimusLogging();
app.MapControllers();
app.Run();
```

### Get your keys (telemetry)
- **Application Insights Connection String**: In Azure Portal, open your Application Insights resource -> “Overview” -> copy “Connection string”.

---

## 5. Configuration (appsettings.json)
Set your log levels, targets (console/file/App Insights), PII masking, and overrides.

### Full Configuration Example

```json
{
  "PrimusLogging": {
    "ApplicationId": "my-api",
    "Environment": "production",
    "MinLevel": 1,
    "Targets": [
      {
        "Type": "console",
        "Pretty": true
      },
      {
        "Type": "file",
        "Path": "logs/app.log",
        "Async": true,
        "MaxFileSize": 10485760,
        "MaxRetainedFiles": 5,
        "CompressRotatedFiles": true
      },
      {
        "Type": "applicationInsights",
        "ConnectionString": "InstrumentationKey=your-key-here"
      }
    ],
    "Pii": {
      "MaskEmails": true,
      "MaskCreditCards": true,
      "MaskSSN": true,
      "CustomSensitiveKeys": ["password", "apiKey", "secret", "token"]
    }
  }
}
```

### Minimal Configuration

```json
{
  "PrimusLogging": {
    "ApplicationId": "my-api",
    "MinLevel": 1,
    "Targets": [
      { "Type": "console", "Pretty": true }
    ]
  }
}
```

### Configuration Options Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ApplicationId` | string | `""` | Application identifier included in all logs |
| `Environment` | string | `"development"` | Environment name (development, production, etc.) |
| `MinLevel` | int | `1` | Minimum log level: 0=Debug, 1=Info, 2=Warning, 3=Error, 4=Critical |
| `Targets` | array | `[{"Type":"console"}]` | List of output targets |

### Target Configuration Reference

| Target Type | Options | Description |
|-------------|---------|-------------|
| `console` | `Pretty` (bool) | Colored console output for development |
| `file` | `Path`, `Async`, `MaxFileSize`, `MaxRetainedFiles`, `CompressRotatedFiles` | File output with rotation |
| `applicationInsights` | `ConnectionString` | Azure Application Insights |
| `serilog` | - | Bridge to existing Serilog pipeline |
| `nlog` | - | Bridge to existing NLog configuration |

### PII Options Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaskEmails` | bool | `true` | Mask email addresses in logs |
| `MaskCreditCards` | bool | `true` | Mask credit card numbers |
| `MaskSSN` | bool | `true` | Mask social security numbers |
| `MaskPasswords` | bool | `true` | Mask common password keys (`password`, `pwd`, `pass`) |
| `MaskTokens` | bool | `true` | Mask JWT/bearer tokens and token fields |
| `MaskSecrets` | bool | `true` | Mask secret-related keys (`secret`, `clientSecret`, `connectionString`, `apiKey`, `x-api-key`) |
| `CustomSensitiveKeys` | array | `[]` | Additional field names to mask |
| `CustomRegexPatterns` | array | `[]` | Additional regex patterns to mask in values |

---

## 6. Middleware Pipeline Order

**Critical**: Place `UsePrimusLogging()` early in the pipeline for maximum context capture:

```csharp
var app = builder.Build();

// 1. Exception handling (first)
app.UseExceptionHandler("/error");

// 2. Primus Logging middleware (early for context capture)
app.UsePrimusLogging();

// 3. HTTPS redirection
app.UseHttpsRedirection();

// 4. Static files
app.UseStaticFiles();

// 5. Routing
app.UseRouting();

// 6. CORS
app.UseCors();

// 7. Authentication
app.UseAuthentication();

// 8. Authorization
app.UseAuthorization();

// 9. Endpoints
app.MapControllers();

app.Run();
```

---

## Troubleshooting (field feedback)

- **Config binding ignored/silent**: Use `builder.Logging.AddPrimus(builder.Configuration.GetSection("PrimusLogging"))` and ensure at least one valid target (`console`, `file`, `applicationinsights`) is present. If nothing binds, nothing writes—add validation to throw when targets are missing/invalid.
- **Environment shows Production**: For dev-time Swagger in samples, set `ASPNETCORE_ENVIRONMENT=Development` when running (`dotnet run --urls=http://localhost:5002`).
- **Prefer section overloads**: If available in your package version, use `AddPrimus(IConfigurationSection)` (optionally with an override Action) to avoid manual mapping.

---

## Examples and downloads

- **Minimal**: `examples/logging/Minimal` — [Download zip](/downloads/logging-minimal.zip) — Postman included in the zip.
- **Advanced**: `examples/logging/Advanced` — [Download zip](/downloads/logging-advanced.zip) — Postman included in the zip.
- Swagger (static): [Minimal](/downloads/logging-minimal-swagger.json), [Advanced](/downloads/logging-advanced-swagger.json)
- Full-stack reference: `examples/LiveDemoApi` (Logging + other modules).
- Verified with:
  - Minimal: `cd examples/logging/Minimal && dotnet restore && dotnet run --urls=http://localhost:5002`
  - Advanced: `cd examples/logging/Advanced && dotnet restore && dotnet run --urls=http://localhost:5003`
- Quick curl:
  - `curl http://localhost:5002/ping`

The middleware automatically enriches logs with:
- **Request ID** — From `X-Request-ID` header or auto-generated
- **HTTP Method & Path**
- **Status Code**
- **Response Time**
- **User Context** — If set in `HttpContext.Items["PrimusUser"]`
- **Tenant Context** — If set in `HttpContext.Items["PrimusTenantContext"]`

---

## 7. Required Dependencies

The package automatically includes these dependencies:

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.Extensions.Logging` | 6.0.0+ | Standard logging abstractions |
| `Microsoft.Extensions.Configuration` | 6.0.0+ | Configuration binding |
| `System.Text.Json` | (framework) | JSON serialization |

### Optional Dependencies

Install these only if using specific targets:

| Package | When Needed |
|---------|-------------|
| `Microsoft.ApplicationInsights.AspNetCore` | Application Insights target |
| `Serilog.AspNetCore` | Serilog bridge target |
| `NLog.Web.AspNetCore` | NLog bridge target |

---

## 8. External Guides & Resources

### Azure Application Insights
- [Application Insights overview](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Enable Application Insights for ASP.NET Core](https://learn.microsoft.com/azure/azure-monitor/app/asp-net-core)

### Structured Logging Best Practices
- [Microsoft Logging Guidelines](https://learn.microsoft.com/dotnet/core/extensions/logging)
- [Structured Logging with Serilog](https://github.com/serilog/serilog/wiki/Structured-Data)

### Compliance & PII
- [GDPR and Logging](https://gdpr.eu/article-17-right-to-be-forgotten/)

---

## 9. End-to-End Working Example

### Complete Minimal API Example

**Step 1: Create project and install package**
```bash
dotnet new webapi -n MyLoggingApi
cd MyLoggingApi
dotnet add package PrimusSaaS.Logging
```

**Step 2: Replace `Program.cs`**
```csharp
using PrimusSaaS.Logging.Extensions;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure Primus Logging
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "my-logging-api";
    options.Environment = builder.Environment.EnvironmentName;
    options.MinLevel = PrimusSaaS.Logging.Core.LogLevel.Debug;
    options.Targets = new List<PrimusSaaS.Logging.Core.TargetConfig>
    {
        new() { Type = "console", Pretty = true }
    };
    options.Pii.MaskEmails = true;
});

var app = builder.Build();

// Add logging middleware
app.UsePrimusLogging();

// Public endpoint with logging
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.LogInformation("Home endpoint accessed");
    return "Hello, World!";
});

// Endpoint demonstrating structured logging
app.MapGet("/users/{id}", (string id, ILogger<Program> logger) =>
{
    logger.LogInformation("Fetching user {UserId}", id);
    return new { UserId = id, Name = "John Doe" };
});

// Endpoint demonstrating error logging
app.MapGet("/error", (ILogger<Program> logger) =>
{
    try
    {
        throw new InvalidOperationException("Something went wrong");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing request");
        return Results.Problem("An error occurred");
    }
});

// Endpoint demonstrating PII masking
app.MapPost("/register", (RegisterRequest request, ILogger<Program> logger) =>
{
    // Email will be automatically masked in logs
    logger.LogInformation("Registering user with email: {Email}", request.Email);
    return Results.Ok(new { Message = "User registered" });
});

app.Run();

record RegisterRequest(string Email, string Name);
```

**Step 3: Run and test**
```bash
dotnet run
```

**Step 4: Test with curl**
```bash
# Basic request
curl http://localhost:5000/

# Request with user ID (check console for structured log)
curl http://localhost:5000/users/123

# Error endpoint
curl http://localhost:5000/error

# PII masking test
curl -X POST http://localhost:5000/register \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "name": "John"}'
```

### Controller-Based Example with Context Enrichment

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Set user context for all subsequent logs
        HttpContext.Items["PrimusUser"] = new Dictionary<string, object>
        {
            ["userId"] = request.UserId,
            ["email"] = "masked@example.com"
        };

        _logger.LogInformation(
            "Creating order for user {UserId} with {ItemCount} items",
            request.UserId,
            request.Items.Count);

        // Simulate order processing
        var orderId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Order {OrderId} created successfully",
            orderId);

        return Ok(new { OrderId = orderId });
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(string id)
    {
        _logger.LogDebug("Looking up order {OrderId}", id);
        
        // Simulated lookup
        return Ok(new { OrderId = id, Status = "Processing" });
    }
}

public record CreateOrderRequest(string UserId, List<string> Items);
```

### Performance Tracking Example

```csharp
using PrimusSaaS.Logging.Core;

[HttpPost("process")]
public async Task<IActionResult> ProcessData([FromServices] Logger logger)
{
    var correlationId = logger.GenerateCorrelationId();
    
    logger.Info("Starting data processing", new Dictionary<string, object>
    {
        ["correlationId"] = correlationId
    });

    var timer = logger.StartTimer();

    // Simulate processing
    await Task.Delay(1500);

    timer.Done("Data processing completed", new Dictionary<string, object>
    {
        ["correlationId"] = correlationId,
        ["recordsProcessed"] = 1000
    });
    // Output: "Data processing completed" with durationMs: 1500

    return Ok();
}
```

---

## 10. Troubleshooting

### Common Issues and Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| Logs not appearing | MinLevel too high | Set `MinLevel` to `0` (Debug) for development |
| File not created | Permission issue | Ensure app has write access to logs directory |
| App Insights logs missing | Wrong connection string | Verify `ConnectionString` format and value |
| PII not masked | Feature not enabled | Set `Pii.MaskEmails = true` in configuration |
| Duplicate logs | Multiple providers | Call `builder.Logging.ClearProviders()` first |

### Debug Logging Configuration

Add this to verify logging is configured correctly:

```csharp
var app = builder.Build();

// Log configuration on startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Logging configured with environment: {Env}", 
    builder.Environment.EnvironmentName);
```

### Verify File Target

```csharp
// Check if file target is working
var logsPath = Path.Combine(app.Environment.ContentRootPath, "logs");
if (!Directory.Exists(logsPath))
{
    Directory.CreateDirectory(logsPath);
    Console.WriteLine($"Created logs directory: {logsPath}");
}
```

### Log Level Reference

| Level | Value | Use Case |
|-------|-------|----------|
| Debug | 0 | Detailed diagnostic information |
| Info | 1 | General operational messages |
| Warning | 2 | Unexpected but recoverable situations |
| Error | 3 | Errors that don't stop the application |
| Critical | 4 | Fatal errors requiring immediate attention |

---

## 11. FAQ

### Q: Can I use Primus Logging with existing `ILogger<T>` code?
**A:** Yes! Primus Logging is a drop-in replacement. Just call `builder.Logging.ClearProviders()` and `builder.Logging.AddPrimus()`. All existing `ILogger<T>` injections continue to work.

### Q: How do I bridge to my existing Serilog sinks?
**A:** Add a Serilog target in configuration:
```json
{
  "Targets": [
    { "Type": "serilog" }
  ]
}
```
This forwards enriched logs to your existing Serilog pipeline.

### Q: Does Primus Logging send data to any external service?
**A:** No, unless you configure Application Insights or similar targets. All local targets (console, file) stay within your infrastructure.

### Q: How do I add custom fields to every log entry?
**A:** Use custom enrichers:
```csharp
builder.Logging.AddPrimus(options =>
{
    options.Enrichers.Add(new MyCustomEnricher());
});
```

### Q: What's the performance impact of async file logging?
**A:** Async buffering typically improves throughput by 10-100x compared to synchronous writes. The buffer handles backpressure automatically.

### Q: How do I correlate logs across microservices?
**A:** Use correlation IDs:
```csharp
// Service A: Generate and pass correlation ID
var correlationId = logger.GenerateCorrelationId();
httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

// Service B: Read from header (automatic with UsePrimusLogging middleware)
```

---

## 12. Version Compatibility

| SDK Version | .NET 6 | .NET 7 | .NET 8 | Notes |
|-------------|--------|--------|--------|-------|
| 1.2.4 | ✅ | ✅ | ✅ | Current release |
| 1.2.0 | ✅ | ✅ | ✅ | Added Serilog/NLog bridges |
| 1.1.0 | ✅ | ✅ | ❌ | Initial release |

### Breaking Changes

**v1.2.0**: No breaking changes. Serilog/NLog bridges are additive.

### Upgrading

```bash
dotnet add package PrimusSaaS.Logging --version 1.2.4
```

---

## 13. Next Steps

After integrating Logging Module, consider these complementary modules:

| Module | Purpose | Docs |
|--------|---------|------|
| **[Identity Quick Start](/docs/modules/identity-quick-start)** | Add JWT/OIDC authentication with multi-issuer support | Previous |
| **[Notifications Module](/docs/modules/notifications)** | Send templated emails/SMS with Liquid templates | Next |
| **[Feature Flags](/docs/modules/feature-flags)** | Control feature rollouts with percentage and user targeting | Optional |

### Full Integration Example

See the [Live Demo API](/docs/modules/live-demo-api) for a complete working example with all modules integrated.
