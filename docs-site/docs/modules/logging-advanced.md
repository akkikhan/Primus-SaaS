---
id: logging-advanced
title: Logging Module - Advanced Features
sidebar_position: 21
description: Custom PII masking, correlation IDs, enrichers, and observability integration.
---

# Logging Advanced Features

Unlock custom PII patterns, correlation tracking, log enrichment, and full observability integration.

---

## Baseline wiring

```csharp
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Use Primus logging everywhere
builder.Logging.AddPrimus(opts =>
    builder.Configuration.GetSection("PrimusLogging").Bind(opts));

// Trim default providers if you only want Primus targets (avoids duplicate console output)
// builder.Logging.ClearProviders();

var app = builder.Build();

// Request logging + correlation IDs
app.UsePrimusLogging();
```

`ApplicationId` in `PrimusLogging` helps tag logs per service; set it in config so it appears on every log event.

---

## Custom PII Masking Patterns
Protect sensitive data in logs with built-in and custom regex masks.

### Configure Custom Patterns

```json
{
  "PrimusLogging": {
    "EnablePiiMasking": true,
    "PiiOptions": {
      "MaskEmails": true,
      "MaskCreditCards": true,
      "MaskPhoneNumbers": true,
      "MaskIpAddresses": true,
      "MaskSsn": true,
      "CustomPatterns": [
        {
          "Name": "CustomerId",
          "Pattern": "CID-[A-Z0-9]{8}",
          "MaskWith": "CID-****"
        },
        {
          "Name": "ApiKey",
          "Pattern": "sk_[a-zA-Z0-9]{32}",
          "MaskWith": "sk_****"
        }
      ]
    }
  }
}
```

### PII Masking Examples

| Input | Output |
|-------|--------|
| `john.doe@example.com` | `j***@example.com` |
| `4111-1111-1111-1111` | `****-****-****-1111` |
| `+1-555-123-4567` | `+1-555-***-****` |
| `192.168.1.100` | `192.168.*.***` |
| `123-45-6789` | `***-**-6789` |
| `CID-ABC12345` | `CID-****` |

### Programmatic Masking

```csharp
using PrimusSaaS.Logging.Extensions;

builder.Logging.AddPrimus(opts =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(opts);
    
    // Add custom masking rule
    opts.AddMaskingRule(
        name: "OrderNumber",
        pattern: @"ORD-\d{8}",
        replacement: "ORD-****"
    );
    
    // Conditional masking
    opts.AddMaskingRule(
        name: "InternalId",
        pattern: @"INT-\d+",
        replacement: m => $"INT-{new string('*', m.Length - 4)}"
    );
});
```

---

## Correlation IDs
Trace a request end-to-end by stamping a correlation ID on every log and propagating it downstream.

Track requests across services with automatic correlation ID propagation.

### Enable Correlation IDs

```json
{
  "PrimusLogging": {
    "CorrelationId": {
      "Enabled": true,
      "HeaderName": "X-Correlation-ID",
      "GenerateIfMissing": true,
      "IncludeInResponse": true
    }
  }
}
```

### Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPrimus(opts =>
    builder.Configuration.GetSection("PrimusLogging").Bind(opts));

var app = builder.Build();

// Add correlation middleware
app.UsePrimusCorrelation();

app.MapGet("/", (HttpContext ctx, ILogger<Program> logger) =>
{
    // Correlation ID automatically included in all logs
    logger.LogInformation("Processing request");
    return new { status = "ok" };
});

app.Run();
```

### Log Output with Correlation

```
[2024-01-15 10:30:45 INF] [CorrelationId: abc-123-def] Processing request
[2024-01-15 10:30:45 INF] [CorrelationId: abc-123-def] Fetching data from database
[2024-01-15 10:30:46 INF] [CorrelationId: abc-123-def] Request completed
```

### Propagate to Downstream Services

```csharp
public class OrderService
{
    private readonly HttpClient _httpClient;
    private readonly ICorrelationIdAccessor _correlationAccessor;

    public OrderService(HttpClient httpClient, ICorrelationIdAccessor correlationAccessor)
    {
        _httpClient = httpClient;
        _correlationAccessor = correlationAccessor;
    }

    public async Task<Order> GetOrderAsync(string orderId)
    {
        // Correlation ID automatically added to outgoing requests
        var correlationId = _correlationAccessor.GetCorrelationId();
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        
        return await _httpClient.GetFromJsonAsync<Order>($"/api/orders/{orderId}");
    }
}
```

---

## Log Enrichment
Attach extra context (app/env/version, user, machine, request info) to every log event.

Add contextual information to every log entry.

### Static Enrichers
Add fixed properties to all logs (e.g., app name, environment, version).

```csharp
builder.Logging.AddPrimus(opts =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(opts);
    
    // Add static properties to all logs
    opts.Enrich.WithProperty("Application", "OrderService");
    opts.Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);
    opts.Enrich.WithProperty("Version", "1.2.3");
});
```

### Dynamic Enrichers
Add machine/process/thread info and custom enrichers that read per-request/user context.

```csharp
builder.Logging.AddPrimus(opts =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(opts);
    
    // Add machine info
    opts.Enrich.WithMachineName();
    opts.Enrich.WithThreadId();
    opts.Enrich.WithProcessId();
    
    // Custom dynamic enricher
    opts.Enrich.With<UserContextEnricher>();
});

// Custom enricher implementation
public class UserContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst("sub")?.Value;
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("UserId", userId ?? "unknown"));
        }
    }
}
```

### Request-Scoped Enrichment
Push request-specific properties (path, method, user agent) into all logs for that request.

```csharp
app.Use(async (context, next) =>
{
    // Add request-specific properties to all logs in this request
    using (LogContext.PushProperty("RequestPath", context.Request.Path))
    using (LogContext.PushProperty("RequestMethod", context.Request.Method))
    using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.ToString()))
    {
        await next();
    }
});
```

---

## Noise and safety controls

- `TruncateCategoryNames` / `MaxCategoryLength`: disable category redaction or cap namespace length while preserving the full value in `categoryFull`.
- `SamplingRate` + `AlwaysLogOnError`: sample down noisy info/debug logs; errors/criticals bypass sampling when `AlwaysLogOnError=true`.
- `MaskFields`: forward explicit sensitive keys to the PII masker (e.g., `["password","token","apiKey","ssn"]`).

---

## Structured Logging Best Practices

### Use Message Templates

```csharp
// ✅ Good - structured with named properties
_logger.LogInformation("Order {OrderId} placed by {CustomerId} for {Amount:C}", 
    orderId, customerId, amount);

// ❌ Bad - string interpolation loses structure
_logger.LogInformation($"Order {orderId} placed by {customerId} for {amount:C}");
```

### Use Scopes for Context

```csharp
public async Task ProcessOrderAsync(Order order)
{
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["OrderId"] = order.Id,
        ["CustomerId"] = order.CustomerId,
        ["Region"] = order.Region
    }))
    {
        _logger.LogInformation("Starting order processing");
        
        await ValidateOrder(order);
        _logger.LogInformation("Order validated");
        
        await ChargePayment(order);
        _logger.LogInformation("Payment charged");
        
        await ShipOrder(order);
        _logger.LogInformation("Order shipped");
    }
}
```

### Exception Logging

```csharp
try
{
    await ProcessPayment(order);
}
catch (PaymentException ex)
{
    // ✅ Pass exception as first parameter
    _logger.LogError(ex, "Payment failed for order {OrderId}", order.Id);
    throw;
}
```

---

## Multiple Output Targets

### Console + File + Application Insights

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Information",
    "Targets": ["Console", "File", "ApplicationInsights"],
    "Console": {
      "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
    },
    "File": {
      "Path": "logs/app-.log",
      "RollingInterval": "Day",
      "RetainedFileCount": 30,
      "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
    },
    "ApplicationInsights": {
      "ConnectionString": "InstrumentationKey=xxx",
      "MinimumLevel": "Warning"
    }
  }
}
```

### Conditional Logging by Level

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Debug",
    "LevelOverrides": {
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "System": "Warning",
      "MyApp.Services": "Debug"
    }
  }
}
```

---

## Filtering Sensitive Data

### Exclude Sensitive Endpoints

```csharp
builder.Logging.AddPrimus(opts =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(opts);
    
    // Don't log these paths
    opts.ExcludePaths.Add("/health");
    opts.ExcludePaths.Add("/metrics");
    opts.ExcludePaths.Add("/api/auth/token");  // Token endpoints
});
```

### Filter Request/Response Bodies

```csharp
builder.Logging.AddPrimus(opts =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(opts);
    
    // Don't log request bodies for these endpoints
    opts.ExcludeRequestBodyPaths.Add("/api/users/*/password");
    opts.ExcludeRequestBodyPaths.Add("/api/auth/*");
    
    // Maximum body size to log
    opts.MaxBodyLogSize = 4096;
});
```

---

## OpenTelemetry Integration

```csharp
using OpenTelemetry.Logs;

builder.Logging.AddPrimus(opts => 
    builder.Configuration.GetSection("PrimusLogging").Bind(opts));

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("MyService"));
    options.AddOtlpExporter();
});
```

### Export to Jaeger/Zipkin

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("PrimusSaaS.Logging")
            .AddJaegerExporter(o =>
            {
                o.AgentHost = "localhost";
                o.AgentPort = 6831;
            });
    });
```

---

## Seq Integration

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Information",
    "Targets": ["Console", "Seq"],
    "Seq": {
      "ServerUrl": "http://localhost:5341",
      "ApiKey": "your-api-key"
    }
  }
}
```

---

## Performance Logging

### Log Execution Time

```csharp
public class TimedService
{
    private readonly ILogger<TimedService> _logger;

    public async Task<Result> ProcessAsync()
    {
        using var activity = _logger.BeginTimedOperation("ProcessAsync");
        
        // ... do work ...
        
        return result;
        // Automatically logs: "ProcessAsync completed in 234ms"
    }
}
```

### Conditional Logging

```csharp
// Only evaluate expensive operation if debug is enabled
if (_logger.IsEnabled(LogLevel.Debug))
{
    var debugInfo = GenerateExpensiveDebugInfo();
    _logger.LogDebug("Debug info: {DebugInfo}", debugInfo);
}
```

---

## Health Check Logging

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<LoggingHealthCheck>("logging");

public class LoggingHealthCheck : IHealthCheck
{
    private readonly IPrimusLoggingStatus _loggingStatus;

    public LoggingHealthCheck(IPrimusLoggingStatus loggingStatus)
    {
        _loggingStatus = loggingStatus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var status = _loggingStatus.GetStatus();
        
        if (status.AllTargetsHealthy)
        {
            return Task.FromResult(HealthCheckResult.Healthy("All logging targets operational"));
        }
        
        return Task.FromResult(HealthCheckResult.Degraded(
            $"Unhealthy targets: {string.Join(", ", status.UnhealthyTargets)}"));
    }
}
```

---

## Complete Example

```csharp
using PrimusSaaS.Logging;
using Serilog.Context;

var builder = WebApplication.CreateBuilder(args);

// Configure Primus Logging with all features
builder.Logging.AddPrimus(opts =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(opts);
    
    // Static enrichment
    opts.Enrich.WithProperty("Application", "OrderAPI");
    opts.Enrich.WithProperty("Version", "2.0.0");
    opts.Enrich.WithMachineName();
    
    // Custom masking
    opts.AddMaskingRule("OrderSecret", @"SECRET-\w+", "SECRET-****");
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

var app = builder.Build();

// Add correlation ID middleware
app.UsePrimusCorrelation();

// Add request context to logs
app.Use(async (ctx, next) =>
{
    using (LogContext.PushProperty("RequestId", ctx.TraceIdentifier))
    using (LogContext.PushProperty("ClientIP", ctx.Connection.RemoteIpAddress))
    {
        await next();
    }
});

app.MapControllers();
app.Run();
```

### appsettings.json

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Information",
    "Targets": ["Console", "File", "ApplicationInsights"],
    "CorrelationId": {
      "Enabled": true,
      "HeaderName": "X-Correlation-ID"
    },
    "EnablePiiMasking": true,
    "PiiOptions": {
      "MaskEmails": true,
      "MaskCreditCards": true,
      "MaskPhoneNumbers": true
    },
    "Console": {
      "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
    },
    "File": {
      "Path": "logs/app-.log",
      "RollingInterval": "Day"
    },
    "ApplicationInsights": {
      "ConnectionString": "your-connection-string"
    },
    "LevelOverrides": {
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Basic setup | [Quick Start →](/docs/modules/logging-quick-start) |
| Full reference | [Logging Module Reference →](/docs/modules/logging-module) |
