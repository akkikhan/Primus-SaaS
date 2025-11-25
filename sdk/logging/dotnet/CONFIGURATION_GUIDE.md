# Configuration Guide - PrimusSaaS.Logging

This guide provides comprehensive configuration examples for PrimusSaaS.Logging.

## Table of Contents

- [Basic Configuration](#basic-configuration)
- [Configuration via appsettings.json](#configuration-via-appsettingsjson)
- [Configuration via Code](#configuration-via-code)
- [Target Types](#target-types)
- [PII Masking](#pii-masking)
- [Log Levels](#log-levels)

---

## Basic Configuration

### Minimal Setup

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-APP";
    options.Environment = "development";
});

var app = builder.Build();
app.Run();
```

---

## Configuration via appsettings.json

### Step 1: Add Configuration Section

Add this to your `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "PrimusLogging": {
    "ApplicationId": "my-app",
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
      }
    ],
    "Pii": {
      "MaskEmails": true,
      "MaskCreditCards": true,
      "MaskSSN": true,
      "CustomSensitiveKeys": ["password", "apiKey", "token"]
    }
  }
}
```

### Step 2: Bind Configuration in Program.cs

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(builder.Configuration.GetSection("PrimusLogging"));
// Optional in-code overrides:
// builder.Logging.AddPrimus(builder.Configuration.GetSection("PrimusLogging"), options =>
// {
//     options.MinLevel = LogLevel.Info;
// });

var app = builder.Build();
app.Run();
```

---

## Configuration via Code

### Complete Example

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    // Basic settings
    options.ApplicationId = "my-webapi";
    options.Environment = "production";
    options.MinLevel = LogLevel.Info;
    
    // Configure targets
    options.Targets = new List<TargetConfig>
    {
        // Console with pretty printing
        new TargetConfig
        {
            Type = "console",
            Pretty = true
        },
        
        // File with rotation
        new TargetConfig
        {
            Type = "file",
            Path = "logs/app.log",
            Async = true,
            MaxFileSize = 10 * 1024 * 1024,  // 10MB
            MaxRetainedFiles = 5,
            CompressRotatedFiles = true
        },
        
        // Azure Application Insights
        new TargetConfig
        {
            Type = "applicationInsights",
            ConnectionString = "InstrumentationKey=your-key-here"
        }
    };
    
    // PII Masking
    options.Pii = new PiiOptions
    {
        MaskEmails = true,
        MaskCreditCards = true,
        MaskSSN = true,
        CustomSensitiveKeys = new List<string> { "password", "apiKey", "secret" }
    };
    
    // Custom enrichers
    options.Enrichers = new List<IEnricher>
    {
        new MachineNameEnricher(),
        new ThreadIdEnricher()
    };
});

var app = builder.Build();
app.UsePrimusLogging();  // Add middleware
app.Run();
```

---

## Target Types

### Console Target

**Purpose:** Output logs to console/terminal

```csharp
new TargetConfig
{
    Type = "console",
    Pretty = true  // Human-readable format with colors
}
```

**appsettings.json:**
```json
{
  "Type": "console",
  "Pretty": true
}
```

**Alternative (using Format property):**
```json
{
  "Type": "console",
  "Format": "PrettyPrint"
}
```

### File Target

**Purpose:** Write logs to rotating files

```csharp
new TargetConfig
{
    Type = "file",
    Path = "logs/app.log",
    Async = true,                    // Non-blocking writes
    MaxFileSize = 10 * 1024 * 1024,  // 10MB before rotation
    MaxRetainedFiles = 5,            // Keep 5 old files
    CompressRotatedFiles = true      // Gzip rotated files
}
```

**appsettings.json:**
```json
{
  "Type": "file",
  "Path": "logs/app.log",
  "Async": true,
  "MaxFileSize": 10485760,
  "MaxRetainedFiles": 5,
  "CompressRotatedFiles": true
}
```

### Application Insights Target

**Purpose:** Send logs to Azure Application Insights

```csharp
new TargetConfig
{
    Type = "applicationInsights",
    ConnectionString = "InstrumentationKey=abc123..."
}
```

**appsettings.json:**
```json
{
  "Type": "applicationInsights",
  "ConnectionString": "InstrumentationKey=abc123..."
}
```

---

## PII Masking

### Enable PII Masking

```csharp
options.Pii = new PiiOptions
{
    MaskEmails = true,           // Masks email addresses
    MaskCreditCards = true,      // Masks credit card numbers
    MaskSSN = true,              // Masks social security numbers
    CustomSensitiveKeys = new List<string> 
    { 
        "password", 
        "apiKey", 
        "token",
        "secret"
    }
};
```

### How It Works

**Before masking:**
```json
{
  "message": "User john@example.com logged in",
  "context": {
    "email": "john@example.com",
    "password": "secret123"
  }
}
```

**After masking:**
```json
{
  "message": "User ***REDACTED*** logged in",
  "context": {
    "email": "***REDACTED***",
    "password": "***REDACTED***"
  }
}
```

---

## Log Levels

### Available Levels

| Level | Integer Value | Description |
|-------|---------------|-------------|
| Debug | 0 | Detailed diagnostic information |
| Info | 1 | Informational messages |
| Warning | 2 | Warning messages |
| Error | 3 | Error messages |
| Critical | 4 | Critical failures |

### Setting Minimum Level

**Via Code:**
```csharp
options.MinLevel = LogLevel.Info;  // Only Info and above
```

**Via appsettings.json:**
```json
{
  "MinLevel": 1
}
```

**Note:** Use integer values in JSON configuration:
- `0` = Debug
- `1` = Info
- `2` = Warning
- `3` = Error
- `4` = Critical

---

## Environment-Specific Configuration

### Development

```json
{
  "PrimusLogging": {
    "ApplicationId": "my-app",
    "Environment": "development",
    "MinLevel": 0,
    "Targets": [
      {
        "Type": "console",
        "Pretty": true
      }
    ]
  }
}
```

### Production

```json
{
  "PrimusLogging": {
    "ApplicationId": "my-app",
    "Environment": "production",
    "MinLevel": 1,
    "Targets": [
      {
        "Type": "file",
        "Path": "/var/log/myapp/app.log",
        "Async": true,
        "MaxFileSize": 52428800,
        "MaxRetainedFiles": 10,
        "CompressRotatedFiles": true
      },
      {
        "Type": "applicationInsights",
        "ConnectionString": "InstrumentationKey=..."
      }
    ],
    "Pii": {
      "MaskEmails": true,
      "MaskCreditCards": true,
      "MaskSSN": true
    }
  }
}
```

---

## Troubleshooting

### Issue: Logs not appearing

**Check:**
1. MinLevel is not too high
2. Target configuration is correct
3. File path is writable

### Issue: Configuration not loading

**Check:**
1. Section name is exactly `"PrimusLogging"`
2. JSON is valid
3. Configuration binding code is correct

### Issue: PII not being masked

**Check:**
1. `Pii.MaskEmails` is set to `true`
2. Custom keys are added to `CustomSensitiveKeys`
3. Masking is case-insensitive for keys

---

## Best Practices

1. **Use appsettings.json** for environment-specific configuration
2. **Set appropriate MinLevel** - Debug for dev, Info+ for production
3. **Enable PII masking** in production
4. **Use async file targets** for high-throughput apps
5. **Configure file rotation** to prevent disk exhaustion
6. **Use Application Insights** for production monitoring
