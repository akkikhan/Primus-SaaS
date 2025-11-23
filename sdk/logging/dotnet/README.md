# Primus SaaS Logging SDK for .NET

Enterprise-grade structured logging library for .NET applications with automatic context enrichment and multiple output targets.

## Features

- ✅ **Structured Logging** - JSON-formatted logs with rich context
- ✅ **Log Levels** - DEBUG, INFO, WARNING, ERROR, CRITICAL
- ✅ **Context Enrichment** - Automatic HTTP context, user, and tenant enrichment
- ✅ **Multiple Targets** - Console (with pretty printing), File, Application Insights
- ✅ **Performance Tracking** - Built-in timers for measuring operations
- ✅ **Correlation IDs** - For distributed tracing
- ✅ **ASP.NET Core Integration** - Middleware for automatic request logging
- ✅ **Thread-Safe** - Safe for concurrent use

## Installation

```bash
dotnet add package PrimusSaaS.Logging
```

## Quick Start

### Basic Usage

```csharp
using PrimusSaaS.Logging.Core;

// Create logger
var logger = new Logger(new LoggerOptions
{
    ApplicationId = "MY-APP",
    Environment = "production",
    MinLevel = LogLevel.Info
});

// Log messages
logger.Info("Application started");
logger.Error("Something went wrong", new Dictionary<string, object>
{
    ["errorCode"] = "ERR_001",
    ["userId"] = "12345"
});
```

### ASP.NET Core Integration

```csharp
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Logging
builder.Services.AddPrimusLogging(options =>
{
    options.ApplicationId = "MY-WEBAPI";
    options.Environment = "production";
    options.MinLevel = LogLevel.Info;
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = false },
        new() { Type = "file", Path = "logs/app.log" }
    };
});

var app = builder.Build();

// Use logging middleware
app.UsePrimusLogging();

app.Run();
```

### Using in Controllers

```csharp
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Logging.Core;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly Logger _logger;

    public UsersController(Logger logger)
    {
        _logger = logger;
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(string id)
    {
        _logger.Info("Fetching user", new Dictionary<string, object>
        {
            ["userId"] = id
        });

        // Your logic here...

        return Ok();
    }
}
```

## Configuration

### Logger Options

```csharp
var options = new LoggerOptions
{
    // Required: Application identifier
    ApplicationId = "MY-APP",

    // Environment (development, testing, production)
    Environment = "production",

    // Minimum log level to output
    MinLevel = LogLevel.Info,

    // Output targets
    Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() { Type = "file", Path = "logs/app.log" }
    }
};
```

### Output Targets

#### Console Target

```csharp
new TargetConfig 
{ 
    Type = "console", 
    Pretty = true  // Enable colored output
}
```

#### File Target

```csharp
new TargetConfig 
{ 
    Type = "file", 
    Path = "logs/app.log"  // File path (directory created automatically)
}
```

## Advanced Features

### Performance Tracking

```csharp
var timer = logger.StartTimer();

// Your operation here...
Thread.Sleep(100);

timer.Done("Operation completed", new Dictionary<string, object>
{
    ["operationId"] = "OP-123"
});
// Logs: "Operation completed" with duration in milliseconds
```

### Correlation IDs

```csharp
var correlationId = logger.GenerateCorrelationId();

logger.Info("Step 1", new Dictionary<string, object>
{
    ["correlationId"] = correlationId
});

logger.Info("Step 2", new Dictionary<string, object>
{
    ["correlationId"] = correlationId
});
```

### HTTP Context Enrichment

When using the ASP.NET Core middleware, logs automatically include:

- **Request ID** - From `X-Request-ID` header or auto-generated
- **User Context** - From Primus Identity Validator or ASP.NET Identity
- **Tenant Context** - From Primus multi-tenancy
- **HTTP Method & Path**
- **Status Code**

Example log output:

```json
{
  "timestamp": "2025-11-24T04:00:00.000Z",
  "level": "INFO",
  "message": "User logged in",
  "context": {
    "applicationId": "MY-APP",
    "environment": "production",
    "requestId": "req-abc123",
    "userId": "user-12345",
    "tenantId": "tenant-acme",
    "method": "POST",
    "path": "/api/auth/login"
  }
}
```

## Log Levels

| Level | Value | Description |
|-------|-------|-------------|
| Debug | 0 | Detailed diagnostic information |
| Info | 1 | Informational messages |
| Warning | 2 | Warning messages |
| Error | 3 | Error messages |
| Critical | 4 | Critical failures |

### Log Level Filtering

Only logs at or above the configured `MinLevel` will be output:

```csharp
var logger = new Logger(new LoggerOptions
{
    MinLevel = LogLevel.Warning  // Only WARNING, ERROR, CRITICAL
});

logger.Debug("Not logged");
logger.Info("Not logged");
logger.Warn("Logged!");
logger.Error("Logged!");
```

## Integration with Primus Identity Validator

The logging SDK automatically enriches logs with user and tenant context when using the Primus Identity Validator:

```csharp
// In your middleware/controller
HttpContext.Items["PrimusUser"] = new Dictionary<string, object>
{
    ["userId"] = "user-12345",
    ["email"] = "john@example.com"
};

HttpContext.Items["PrimusTenantContext"] = new Dictionary<string, object>
{
    ["tenantId"] = "tenant-acme"
};

// Logs will automatically include this context
logger.Info("User action performed");
```

## Best Practices

1. **Use Dependency Injection** - Register logger as singleton in ASP.NET Core
2. **Set Appropriate Log Levels** - Use DEBUG for development, INFO+ for production
3. **Include Context** - Always add relevant context data to logs
4. **Use Correlation IDs** - For tracking requests across services
5. **Avoid Logging Sensitive Data** - Use PII masking (coming in Milestone 3)

## Examples

See the `Examples/` directory for:
- `BasicUsage/` - Console application example
- `WebApiExample/` - ASP.NET Core Web API integration

## Roadmap

- ✅ Core logging functionality
- ✅ ASP.NET Core integration
- ⏳ PII masking (Milestone 3)
- ⏳ File rotation & compression (Milestone 3)
- ⏳ Azure Application Insights integration (Milestone 3)
- ⏳ Async buffering (Milestone 3)

## License

MIT License - See LICENSE file for details

## Support

For issues and questions:
- GitHub Issues: https://github.com/primus-saas/logging
- Documentation: https://docs.primus-saas.com/logging
