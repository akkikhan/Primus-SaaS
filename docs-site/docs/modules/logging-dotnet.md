# Logging SDK - .NET Quick Start

Enterprise-grade structured logging with PII masking and file rotation.

## Installation

```bash
dotnet add package PrimusSaaS.Logging
```

## Standard ILogger Integration (Recommended)

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Replace default logging with Primus
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-APP";
    options.Environment = "production";
    options.Targets = new List<PrimusSaaS.Logging.Core.TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() 
        { 
            Type = "file", 
            Path = "logs/app.log",
            Async = true,
            MaxFileSize = 10 * 1024 * 1024,
            CompressRotatedFiles = true
        }
    };
    
    // Enable PII masking
    options.Pii.MaskEmails = true;
    options.Pii.MaskCreditCards = true;
});

var app = builder.Build();
app.Run();
```

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
        _logger.LogInformation("User {UserId} from {IP}", "user-123", "192.168.1.1");
        return Ok();
    }
}
```

## Enterprise Features

- **PII Masking**: Automatic redaction of emails, credit cards, SSNs
- **File Rotation**: Size-based rotation with gzip compression
- **Async Buffering**: Non-blocking writes for high performance
- **Application Insights**: Direct Azure Monitor integration
- **Custom Enrichers**: Add dynamic context to every log

## Next Steps

- [Configuration Guide](./logging-configuration)
- [Enterprise Features](./logging-enterprise-features)
- [Targets & Outputs](./logging-targets)
