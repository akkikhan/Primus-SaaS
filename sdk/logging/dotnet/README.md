# PrimusSaaS.Logging - Enterprise Logging for .NET

Enterprise-grade structured logging library for .NET applications with automatic context enrichment, PII masking, and multiple output targets.

## Features

- ✅ **Structured Logging** - JSON-formatted logs with rich context
- ✅ **Log Levels** - DEBUG, INFO, WARNING, ERROR, CRITICAL
- ✅ **Multiple Targets** - Console, File, Azure Application Insights
- ✅ **PII Masking** - Automatic redaction of sensitive data
- ✅ **File Rotation** - Size-based rotation with gzip compression
- ✅ **Async Buffering** - High-performance non-blocking logging
- ✅ **Custom Enrichers** - Add dynamic context to every log
- ✅ **Standard ILogger** - Full compatibility with Microsoft.Extensions.Logging
- ✅ **ASP.NET Core Integration** - Middleware for automatic HTTP context enrichment
- ✅ **Thread-Safe** - Safe for concurrent use

## Installation

```bash
dotnet add package PrimusSaaS.Logging
```

## Quick Start

### Option 1: Standard ILogger (Recommended)

Use the familiar `ILogger<T>` interface:

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Extensions;
// Alias to avoid conflict with Microsoft.Extensions.Logging.LogLevel
using PrimusLogLevel = PrimusSaaS.Logging.Core.LogLevel;

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with PrimusSaaS.Logging
// Both AddPrimus() and AddPrimusLogging() work (aliases)
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-APP";
    options.Environment = "production";
    
    // Use PrimusLogLevel to avoid ambiguity if Microsoft.Extensions.Logging is imported
    options.MinLevel = PrimusLogLevel.Info;
    
    options.Targets = new List<PrimusSaaS.Logging.Core.TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() { Type = "file", Path = "logs/app.log", Async = true }
    };
});

var app = builder.Build();

// Optional: Add middleware for automatic HTTP context enrichment
// Requires: using PrimusSaaS.Logging.Extensions;
app.UsePrimusLogging();

app.Run();

// Use in controllers
[ApiController]
public class MyController : ControllerBase
{
    private readonly ILogger<MyController> _logger;
    
    public MyController(ILogger<MyController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Request received");
        _logger.LogInformation("User {UserId} from {IP}", "user-123", "192.168.1.1");
        return Ok();
    }
}
```

### Option 2: Direct Logger

Use the PrimusSaaS Logger class directly:

```csharp
using PrimusSaaS.Logging.Core;
// If Microsoft.Extensions.Logging is NOT used in this file, LogLevel is unambiguous
// Otherwise use PrimusSaaS.Logging.Core.LogLevel

var logger = new Logger(new LoggerOptions
{
    ApplicationId = "MY-APP",
    Environment = "production",
    MinLevel = LogLevel.Info
});

logger.Info("Application started");
logger.Error("Something went wrong", new Dictionary<string, object>
{
    ["errorCode"] = "ERR_001",
    ["userId"] = "12345"
});
```

## Configuration

### Logger Options

```csharp
var options = new LoggerOptions
{
    // Application identifier
    ApplicationId = "MY-APP",

    // Environment (development, testing, production)
    Environment = "production",

    // Minimum log level (use fully qualified name if needed)
    MinLevel = PrimusSaaS.Logging.Core.LogLevel.Info,

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
    Pretty = true  // Colored output for development
}
```

#### File Target

```csharp
new TargetConfig 
{ 
    Type = "file", 
    Path = "logs/app.log",
    Async = true,                    // Non-blocking writes
    MaxFileSize = 10 * 1024 * 1024,  // 10MB
    MaxRetainedFiles = 5,            // Keep 5 old files
    CompressRotatedFiles = true      // Gzip old files
}
```

#### Azure Application Insights

```csharp
new TargetConfig 
{ 
    Type = "applicationInsights", 
    ConnectionString = "InstrumentationKey=..."
}
```

## Enterprise Features

### PII Masking

Automatically redact sensitive information:

```csharp
builder.Logging.AddPrimus(options =>
{
    options.Pii.MaskEmails = true;
    options.Pii.MaskCreditCards = true;
    options.Pii.MaskSSN = true;
    options.Pii.CustomSensitiveKeys.Add("password");
    options.Pii.CustomSensitiveKeys.Add("apiKey");
});
```

### Custom Enrichers

Add dynamic context to every log:

```csharp
public class MachineNameEnricher : IEnricher
{
    public void Enrich(Dictionary<string, object> context)
    {
        context["machineName"] = Environment.MachineName;
    }
}

options.Enrichers.Add(new MachineNameEnricher());
options.Enrichers.Add(new ThreadIdEnricher());
```

### Performance Tracking

```csharp
var timer = logger.StartTimer();

// Your operation
await ProcessData();

timer.Done("Data processed", new Dictionary<string, object>
{
    ["recordCount"] = 1000
});
// Logs: "Data processed" with duration in milliseconds
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

## ASP.NET Core Integration

### Middleware

The middleware automatically enriches logs with HTTP context:

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Logging to the logging pipeline
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-WEBAPI";
    options.Environment = "production";
});

var app = builder.Build();

// Add middleware for automatic HTTP context enrichment
app.UsePrimusLogging();

app.Run();
```

Logs will automatically include:
- **Request ID** - From `X-Request-ID` header or auto-generated
- **HTTP Method & Path**
- **Status Code**
- **User Context** - If set in `HttpContext.Items["PrimusUser"]`
- **Tenant Context** - If set in `HttpContext.Items["PrimusTenantContext"]`

### Manual Context Enrichment

```csharp
// In your authentication middleware
HttpContext.Items["PrimusUser"] = new Dictionary<string, object>
{
    ["userId"] = "user-12345",
    ["email"] = "john@example.com"
};

HttpContext.Items["PrimusTenantContext"] = new Dictionary<string, object>
{
    ["tenantId"] = "tenant-acme",
    ["tenantName"] = "Acme Corporation"
};

// All subsequent logs will include this context
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

## Best Practices

1. **Use Standard ILogger** - For ecosystem compatibility
2. **Set Appropriate Log Levels** - DEBUG for development, INFO+ for production
3. **Include Context** - Always add relevant context data
4. **Use Correlation IDs** - For tracking requests across services
5. **Enable PII Masking** - Protect sensitive data in production
6. **Use Async Targets** - For high-throughput applications
7. **Configure File Rotation** - Prevent disk space exhaustion

## Examples

See the `Examples/` directory:
- `BasicUsage/` - Console application
- `WebApiExample/` - ASP.NET Core Web API
- `StandardLoggerExample/` - Using ILogger interface

## Performance

- **Async Buffering**: Non-blocking writes with configurable buffer size
- **Thread-Safe**: Lock-free reads, minimal contention
- **File Rotation**: Automatic cleanup prevents disk exhaustion
- **Compression**: Gzip reduces storage by ~70%

## Documentation

- [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md) - Detailed configuration options
- [CUSTOM_ENRICHERS_GUIDE.md](./PrimusSaaS.Logging/CUSTOM_ENRICHERS_GUIDE.md) - Guide to creating custom enrichers
- [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) - Common issues and solutions
- [VERIFICATION_GUIDE.md](./VERIFICATION_GUIDE.md) - Verification steps

## License

MIT License - See LICENSE file for details

## Support

For issues and questions:
- GitHub Issues: https://github.com/primus-saas/logging
- Documentation: https://docs.primus-saas.com/logging
