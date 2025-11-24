# Logging Middleware

HTTP context enrichment for PrimusSaaS.Logging

## Overview

The `UsePrimusLogging()` middleware automatically enriches your logs with HTTP context information including request IDs, user context, and request details.

:::info Version Requirement
Middleware requires **PrimusSaaS.Logging 1.1.0** or later.
:::

## Quick Start

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Logging
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-APP";
    options.Environment = "production";
});

var app = builder.Build();

// Add middleware
app.UsePrimusLogging();

app.MapControllers();
app.Run();
```

## Features

The middleware automatically adds the following to your logs:

### Request ID

- Extracts from `X-Request-ID` header if present
- Auto-generates if not provided: `req-{guid}`
- Adds to response headers for client tracking

```json
{
  "context": {
    "requestId": "req-abc123..."
  }
}
```

### HTTP Context

- HTTP method (GET, POST, etc.)
- Request path
- Query string
- Status code
- Request duration

```json
{
  "context": {
    "method": "GET",
    "path": "/api/users",
    "queryString": "?page=1",
    "statusCode": 200
  }
}
```

### User Context

Automatically extracts user information from authenticated requests:

```json
{
  "context": {
    "userId": "user-123",
    "email": "john@example.com"
  }
}
```

## Middleware Order

Place `UsePrimusLogging()` **after authentication** but **before** your endpoints:

```csharp
var app = builder.Build();

app.UseAuthentication();      // 1. Authenticate first
app.UsePrimusLogging();       // 2. Then add logging middleware
app.UseAuthorization();       // 3. Then authorize

app.MapControllers();
app.Run();
```

## Integration with Identity.Validator

The middleware automatically integrates with PrimusSaaS.Identity.Validator:

```csharp
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Identity Validator
builder.Services.AddPrimusIdentity(options => { /* ... */ });

// Add Logging
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options => { /* ... */ });

var app = builder.Build();

// Middleware order matters!
app.UseAuthentication();      // Identity.Validator sets user claims
app.UsePrimusLogging();       // Logging extracts user from claims
app.UseAuthorization();

app.MapControllers();
app.Run();
```

The middleware will automatically extract:
- `userId` from `sub` or `userId` claim
- `email` from `email` claim

## Manual Context Enrichment

You can manually add context to logs:

```csharp
app.MapGet("/custom", (HttpContext context, ILogger<Program> logger) =>
{
    // Add custom user context
    context.Items["PrimusUser"] = new Dictionary<string, object>
    {
        ["userId"] = "user-123",
        ["email"] = "test@example.com",
        ["role"] = "admin"
    };
    
    // Add tenant context
    context.Items["PrimusTenantContext"] = new Dictionary<string, object>
    {
        ["tenantId"] = "tenant-acme",
        ["tenantName"] = "Acme Corporation"
    };
    
    logger.LogInformation("Custom endpoint called");
    
    return Results.Ok("Success");
});
```

## Example Log Output

### Without Middleware

```json
{
  "timestamp": "2025-11-24T02:30:00.000Z",
  "level": "INFO",
  "message": "User endpoint called",
  "context": {
    "applicationId": "MY-APP",
    "environment": "production"
  }
}
```

### With Middleware

```json
{
  "timestamp": "2025-11-24T02:30:00.000Z",
  "level": "INFO",
  "message": "User endpoint called",
  "context": {
    "applicationId": "MY-APP",
    "environment": "production",
    "requestId": "req-abc123...",
    "method": "GET",
    "path": "/api/users",
    "userId": "user-123",
    "email": "john@example.com"
  }
}
```

## Configuration

The middleware works with all logging targets:

```csharp
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-APP";
    options.Environment = "production";
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() { Type = "file", Path = "logs/app.log" }
    };
});

var app = builder.Build();
app.UsePrimusLogging();  // Enriches all targets
```

## Request Logging

The middleware automatically logs:

### Request Start (DEBUG level)

```json
{
  "level": "DEBUG",
  "message": "GET /api/users",
  "context": {
    "method": "GET",
    "path": "/api/users",
    "queryString": "?page=1"
  }
}
```

### Request Completion (INFO level)

```json
{
  "level": "INFO",
  "message": "Request completed with status 200",
  "context": {
    "statusCode": 200,
    "method": "GET",
    "path": "/api/users",
    "duration": 0
  }
}
```

### Unhandled Exceptions (ERROR level)

```json
{
  "level": "ERROR",
  "message": "Unhandled exception: NullReferenceException",
  "context": {
    "exception": "NullReferenceException",
    "message": "Object reference not set...",
    "stackTrace": "..."
  }
}
```

## Best Practices

1. **Place after authentication** - Ensures user context is available
2. **Use with PII masking** - Protect sensitive user data
3. **Set appropriate log levels** - DEBUG for development, INFO+ for production
4. **Monitor request IDs** - Track requests across services
5. **Use correlation IDs** - For distributed tracing

## Troubleshooting

### Middleware not enriching logs

**Check:**
1. Middleware is added: `app.UsePrimusLogging()`
2. Middleware is after authentication
3. User is authenticated (for user context)

### Request ID not appearing

**Verify:**
```csharp
[HttpGet]
public IActionResult Test()
{
    var requestId = HttpContext.Items["PrimusRequestId"];
    return Ok(new { requestId });
}
```

Check response headers for `X-Request-ID`.

## See Also

- [Logging (.NET)](./logging-dotnet.md)
- [Configuration Guide](./logging-configuration.md)
- [Enterprise Features](./logging-enterprise-features.md)
