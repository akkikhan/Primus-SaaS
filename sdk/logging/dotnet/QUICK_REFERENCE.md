# PrimusSaaS.Logging - Quick Reference

## Installation

```bash
dotnet add package PrimusSaaS.Logging --version 1.1.0
```

---

## Basic Setup

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

// Add middleware (optional)
app.UsePrimusLogging();

app.Run();
```

---

## Configuration Options

### Minimal
```csharp
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-APP";
    options.Environment = "development";
});
```

### Complete
```csharp
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-APP";
    options.Environment = "production";
    options.MinLevel = LogLevel.Info;
    
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() { Type = "file", Path = "logs/app.log", Async = true }
    };
    
    options.Pii = new PiiOptions
    {
        MaskEmails = true,
        MaskCreditCards = true,
        MaskSSN = true
    };
});
```

---

## Usage in Controllers

```csharp
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
        _logger.LogInformation("User {UserId}", "123");
        return Ok();
    }
}
```

---

## Log Levels

| Method | Level | Use For |
|--------|-------|---------|
| `LogDebug()` | 0 | Detailed diagnostics |
| `LogInformation()` | 1 | General info |
| `LogWarning()` | 2 | Warnings |
| `LogError()` | 3 | Errors |
| `LogCritical()` | 4 | Critical failures |

---

## Targets

### Console
```csharp
new TargetConfig { Type = "console", Pretty = true }
```

### File
```csharp
new TargetConfig 
{ 
    Type = "file", 
    Path = "logs/app.log",
    Async = true,
    MaxFileSize = 10 * 1024 * 1024,  // 10MB
    MaxRetainedFiles = 5
}
```

### Application Insights
```csharp
new TargetConfig 
{ 
    Type = "applicationInsights",
    ConnectionString = "InstrumentationKey=..."
}
```

---

## PII Masking

```csharp
options.Pii = new PiiOptions
{
    MaskEmails = true,
    MaskCreditCards = true,
    MaskSSN = true,
    CustomSensitiveKeys = new List<string> { "password", "apiKey" }
};
```

**Result:** Sensitive data → `***REDACTED***`

---

## Middleware

```csharp
app.UsePrimusLogging();
```

**Adds to logs:**
- Request ID
- HTTP method & path
- Status code
- User context (if authenticated)

---

## Common Issues

### Issue: Build warnings about .NET version
**Solution:** Update to v1.1.0
```bash
dotnet add package PrimusSaaS.Logging --version 1.1.0
```

### Issue: "AddPrimusLogging" not found
**Solution:** Use correct namespace
```csharp
using PrimusSaaS.Logging.Extensions;
builder.Logging.AddPrimus(options => { ... });
```

### Issue: Logs not appearing
**Solution:** Check MinLevel
```csharp
options.MinLevel = LogLevel.Debug;  // Allow all logs
```

---

## Documentation

- **README.md** - Quick start
- **CONFIGURATION_GUIDE.md** - Complete config reference
- **TROUBLESHOOTING.md** - Common issues
- **VERIFICATION_GUIDE.md** - How to verify features

---

## API Aliases

Both work identically:
```csharp
builder.Logging.AddPrimus(options => { ... });
builder.Logging.AddPrimusLogging(options => { ... });
```

---

## Example: Full Setup

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "my-api";
    options.Environment = builder.Environment.EnvironmentName;
    options.MinLevel = LogLevel.Information;
    
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() 
        { 
            Type = "file", 
            Path = "logs/app.log",
            Async = true,
            MaxFileSize = 10 * 1024 * 1024,
            MaxRetainedFiles = 5,
            CompressRotatedFiles = true
        }
    };
    
    options.Pii = new PiiOptions
    {
        MaskEmails = true,
        MaskCreditCards = true,
        MaskSSN = true,
        CustomSensitiveKeys = new List<string> { "password" }
    };
});

var app = builder.Build();

app.UsePrimusLogging();
app.MapControllers();
app.Run();
```

---

## Support

- **GitHub:** https://github.com/primus-saas/logging
- **Docs:** https://docs.primus-saas.com/logging
- **Issues:** https://github.com/primus-saas/logging/issues
