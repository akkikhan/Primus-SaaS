# Verification Guide - PrimusSaaS.Logging

This guide helps you verify that PrimusSaaS.Logging features are working correctly.

## Table of Contents

- [Verifying Installation](#verifying-installation)
- [Verifying Basic Logging](#verifying-basic-logging)
- [Verifying PII Masking](#verifying-pii-masking)
- [Verifying File Rotation](#verifying-file-rotation)
- [Verifying Middleware](#verifying-middleware)
- [Verifying Enrichers](#verifying-enrichers)
- [Verifying Performance Tracking](#verifying-performance-tracking)

---

## Verifying Installation

### Step 1: Check Package Version

```bash
dotnet list package | grep PrimusSaaS.Logging
```

**Expected output:**
```
PrimusSaaS.Logging    1.2.4
```

### Step 2: Verify No Build Warnings

```bash
dotnet build
```

**Expected:** No warnings about .NET version compatibility.

If you see warnings, you're using an older version. Update:
```bash
dotnet add package PrimusSaaS.Logging --version 1.2.4
```

---

## Verifying Basic Logging

### Test Console Logging

Create a minimal test:

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "TEST-APP";
    options.Environment = "development";
    options.MinLevel = LogLevel.Debug;
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true }
    };
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Test all log levels
logger.LogDebug("This is a DEBUG message");
logger.LogInformation("This is an INFO message");
logger.LogWarning("This is a WARNING message");
logger.LogError("This is an ERROR message");
logger.LogCritical("This is a CRITICAL message");

app.Run();
```

**Expected console output:**
```
[HH:MM:SS] DEBUG: This is a DEBUG message
{
  "applicationId": "TEST-APP",
  "environment": "development"
}

[HH:MM:SS] INFO: This is an INFO message
{
  "applicationId": "TEST-APP",
  "environment": "development"
}

[HH:MM:SS] WARNING: This is a WARNING message
...
```

### Test File Logging

```csharp
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "TEST-APP";
    options.Environment = "development";
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "file", Path = "logs/test.log" }
    };
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Test file logging");

// Important: Wait for async flush
await Task.Delay(1000);
```

**Verify:**
1. Check that `logs/test.log` file was created
2. Open the file and verify JSON content:

```json
{"timestamp":"2025-11-24T02:30:00.000Z","level":"INFO","message":"Test file logging","context":{"applicationId":"TEST-APP","environment":"development"}}
```

---

## Verifying PII Masking

### Test Email Masking

```csharp
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "PII-TEST";
    options.Environment = "development";
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true }
    };
    options.Pii = new PiiOptions
    {
        MaskEmails = true,
        MaskCreditCards = true,
        MaskSSN = true
    };
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Test email masking
logger.LogInformation("User email: john.doe@example.com");
logger.LogInformation("Contact: {Email}", "jane.smith@company.org");

// Test credit card masking
logger.LogInformation("Payment with card: 4532-1234-5678-9010");

// Test SSN masking
logger.LogInformation("SSN: 123-45-6789");
```

**Expected output:**
```
[HH:MM:SS] INFO: User email: ***REDACTED***
[HH:MM:SS] INFO: Contact: ***REDACTED***
[HH:MM:SS] INFO: Payment with card: ***REDACTED***
[HH:MM:SS] INFO: SSN: ***REDACTED***
```

### Test Custom Sensitive Keys

```csharp
options.Pii = new PiiOptions
{
    MaskEmails = true,
    CustomSensitiveKeys = new List<string> { "password", "apiKey", "secret" }
};

// Later in code
logger.LogInformation("Credentials", new Dictionary<string, object>
{
    ["username"] = "john",
    ["password"] = "secret123",
    ["apiKey"] = "abc-xyz-123"
});
```

**Expected output:**
```json
{
  "message": "Credentials",
  "context": {
    "username": "john",
    "password": "***REDACTED***",
    "apiKey": "***REDACTED***"
  }
}
```

### Verification Checklist

✅ **Email masking works:** `test@example.com` → `***REDACTED***`  
✅ **Credit card masking works:** `4532-1234-5678-9010` → `***REDACTED***`  
✅ **SSN masking works:** `123-45-6789` → `***REDACTED***`  
✅ **Custom keys masked:** `password`, `apiKey` → `***REDACTED***`  
✅ **Non-sensitive data NOT masked:** Regular text remains unchanged

---

## Verifying File Rotation

### Test File Size Rotation

```csharp
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "ROTATION-TEST";
    options.Environment = "development";
    options.Targets = new List<TargetConfig>
    {
        new() 
        { 
            Type = "file", 
            Path = "logs/rotation-test.log",
            MaxFileSize = 1024,  // 1KB for testing
            MaxRetainedFiles = 3,
            CompressRotatedFiles = false  // Easier to verify
        }
    };
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Generate enough logs to trigger rotation
for (int i = 0; i < 100; i++)
{
    logger.LogInformation("Log entry {Index} with some padding text to increase size", i);
    await Task.Delay(10);
}

await Task.Delay(1000);  // Wait for async flush
```

**Verify:**
1. Check `logs/` directory
2. You should see multiple files:
   - `rotation-test.log` (current)
   - `rotation-test.1.log` (previous)
   - `rotation-test.2.log` (older)
   - `rotation-test.3.log` (oldest)

3. Verify old files are deleted when limit is reached

### Test Compression

```csharp
new TargetConfig
{
    Type = "file",
    Path = "logs/compressed-test.log",
    MaxFileSize = 1024,
    CompressRotatedFiles = true
}
```

**Verify:**
- Rotated files have `.gz` extension
- Files are smaller than uncompressed versions

---

## Verifying Middleware

### Test HTTP Context Enrichment

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MIDDLEWARE-TEST";
    options.Environment = "development";
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true }
    };
});

var app = builder.Build();

// Add middleware
app.UsePrimusLogging();

app.MapGet("/test", (ILogger<Program> logger) =>
{
    logger.LogInformation("Test endpoint called");
    return Results.Ok("Success");
});

app.Run();
```

**Test:**
```bash
curl -H "X-Request-ID: test-123" http://localhost:5000/test
```

**Expected output:**
```json
{
  "timestamp": "2025-11-24T02:30:00.000Z",
  "level": "INFO",
  "message": "Test endpoint called",
  "context": {
    "applicationId": "MIDDLEWARE-TEST",
    "environment": "development",
    "requestId": "test-123",
    "method": "GET",
    "path": "/test"
  }
}
```

### Test User Context Enrichment

```csharp
app.MapGet("/user-test", (HttpContext context, ILogger<Program> logger) =>
{
    // Simulate authenticated user
    context.Items["PrimusUser"] = new Dictionary<string, object>
    {
        ["userId"] = "user-123",
        ["email"] = "test@example.com"
    };
    
    logger.LogInformation("User endpoint called");
    return Results.Ok("Success");
});
```

**Expected output:**
```json
{
  "message": "User endpoint called",
  "context": {
    "userId": "user-123",
    "email": "test@example.com",
    "requestId": "req-...",
    "method": "GET",
    "path": "/user-test"
  }
}
```

### Verification Checklist

✅ **Request ID captured:** Either from header or auto-generated  
✅ **HTTP method logged:** GET, POST, etc.  
✅ **Path logged:** Request path  
✅ **User context included:** When PrimusUser is set  
✅ **Response header set:** `X-Request-ID` in response

---

## Verifying Enrichers

### Test Custom Enricher

```csharp
using PrimusSaaS.Logging.Core;

public class MachineNameEnricher : IEnricher
{
    public void Enrich(Dictionary<string, object> context)
    {
        context["machineName"] = Environment.MachineName;
    }
}

public class ThreadIdEnricher : IEnricher
{
    public void Enrich(Dictionary<string, object> context)
    {
        context["threadId"] = Thread.CurrentThread.ManagedThreadId;
    }
}

// In configuration
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "ENRICHER-TEST";
    options.Environment = "development";
    options.Enrichers = new List<IEnricher>
    {
        new MachineNameEnricher(),
        new ThreadIdEnricher()
    };
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true }
    };
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Testing enrichers");
```

**Expected output:**
```json
{
  "message": "Testing enrichers",
  "context": {
    "applicationId": "ENRICHER-TEST",
    "environment": "development",
    "machineName": "YOUR-MACHINE-NAME",
    "threadId": 1
  }
}
```

### Verification Checklist

✅ **Machine name appears:** In every log entry  
✅ **Thread ID appears:** In every log entry  
✅ **Custom enrichers work:** All enrichers are called

---

## Verifying Performance Tracking

### Test Timer

```csharp
using PrimusSaaS.Logging.Core;

var logger = new Logger(new LoggerOptions
{
    ApplicationId = "TIMER-TEST",
    Environment = "development",
    Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true }
    }
});

var timer = logger.StartTimer();

// Simulate work
await Task.Delay(500);

timer.Done("Operation completed", new Dictionary<string, object>
{
    ["recordCount"] = 100
});
```

**Expected output:**
```json
{
  "message": "Operation completed",
  "context": {
    "duration": 500,
    "recordCount": 100
  }
}
```

### Verification Checklist

✅ **Duration logged:** In milliseconds  
✅ **Custom context included:** Additional data present  
✅ **Accurate timing:** Duration matches actual delay

---

## Verifying Correlation IDs

### Test Correlation ID

```csharp
var logger = new Logger(new LoggerOptions
{
    ApplicationId = "CORRELATION-TEST",
    Environment = "development"
});

var correlationId = logger.GenerateCorrelationId();

logger.Info("Step 1: Starting process", new Dictionary<string, object>
{
    ["correlationId"] = correlationId
});

await Task.Delay(100);

logger.Info("Step 2: Processing data", new Dictionary<string, object>
{
    ["correlationId"] = correlationId
});

await Task.Delay(100);

logger.Info("Step 3: Completed", new Dictionary<string, object>
{
    ["correlationId"] = correlationId
});
```

**Expected output:**
```json
{"message":"Step 1: Starting process","context":{"correlationId":"corr-abc123..."}}
{"message":"Step 2: Processing data","context":{"correlationId":"corr-abc123..."}}
{"message":"Step 3: Completed","context":{"correlationId":"corr-abc123..."}}
```

### Verification Checklist

✅ **Correlation ID generated:** Format: `corr-{guid}`  
✅ **Same ID across logs:** All related logs share the ID  
✅ **Searchable:** Can filter logs by correlation ID

---

## Complete Integration Test

Here's a comprehensive test that verifies all features:

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "FULL-TEST";
    options.Environment = "development";
    options.MinLevel = LogLevel.Debug;
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true },
        new() { Type = "file", Path = "logs/full-test.log" }
    };
    options.Pii = new PiiOptions
    {
        MaskEmails = true,
        CustomSensitiveKeys = new List<string> { "password" }
    };
    options.Enrichers = new List<IEnricher>
    {
        new MachineNameEnricher()
    };
});

var app = builder.Build();
app.UsePrimusLogging();

app.MapGet("/full-test", (HttpContext context, ILogger<Program> logger) =>
{
    context.Items["PrimusUser"] = new Dictionary<string, object>
    {
        ["userId"] = "test-user",
        ["email"] = "test@example.com"
    };
    
    logger.LogInformation("Full integration test");
    logger.LogInformation("Sensitive data: {Email}, {Password}", 
        "john@example.com", "secret123");
    
    return Results.Ok(new 
    { 
        status = "success",
        requestId = context.Items["PrimusRequestId"]
    });
});

app.Run();
```

**Test:**
```bash
curl http://localhost:5000/full-test
```

**Verify:**
1. ✅ Console shows pretty-printed logs
2. ✅ File `logs/full-test.log` contains JSON logs
3. ✅ Email is masked: `***REDACTED***`
4. ✅ Password is masked: `***REDACTED***`
5. ✅ Machine name is in context
6. ✅ User context is in logs
7. ✅ Request ID is generated and returned

---

## Troubleshooting Verification

If any verification fails, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for detailed debugging steps.

---

## Summary

Use this checklist to verify your installation:

- [ ] Package installed without warnings
- [ ] Basic console logging works
- [ ] File logging works
- [ ] PII masking works (emails, credit cards, SSN)
- [ ] Custom sensitive keys are masked
- [ ] File rotation works
- [ ] Middleware enriches HTTP context
- [ ] User context is captured
- [ ] Custom enrichers work
- [ ] Performance timer works
- [ ] Correlation IDs work

If all items are checked, PrimusSaaS.Logging is working correctly! 🎉
