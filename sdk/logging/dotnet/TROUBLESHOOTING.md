# Troubleshooting Guide - PrimusSaaS.Logging

This guide helps you diagnose and fix common issues with PrimusSaaS.Logging.

## Table of Contents

- [Installation Issues](#installation-issues)
- [Configuration Issues](#configuration-issues)
- [Logging Not Working](#logging-not-working)
- [Middleware Issues](#middleware-issues)
- [PII Masking Issues](#pii-masking-issues)
- [Performance Issues](#performance-issues)
- [Integration Issues](#integration-issues)

---

## Installation Issues

### Issue: Build Warnings About .NET Version

**Symptoms:**
```
warning : Microsoft.Extensions.Logging 10.0.0 doesn't support net7.0
```

**Cause:** Package dependency version mismatch

**Solution:** This has been fixed in version 1.1.0+. Update to the latest version:
```bash
dotnet add package PrimusSaaS.Logging --version 1.1.0
```

If you're still seeing warnings, clean and rebuild:
```bash
dotnet clean
dotnet restore
dotnet build
```

---

## Configuration Issues

### Issue: "AddPrimusLogging" Not Found

**Symptoms:**
```
error CS1061: 'IServiceCollection' does not contain a definition for 'AddPrimusLogging'
```

**Cause:** Incorrect usage - this is a logging builder extension, not a service collection extension.

**Solution:** Use the correct API:

❌ **Wrong:**
```csharp
builder.Services.AddPrimusLogging(options => { ... });
```

✅ **Correct:**
```csharp
builder.Logging.AddPrimus(options => { ... });
// OR
builder.Logging.AddPrimusLogging(options => { ... });  // Alias
```

### Issue: Configuration Not Loading from appsettings.json

**Symptoms:** Logs don't reflect settings in appsettings.json

**Cause:** Configuration not bound properly

**Solution:** Ensure you're binding configuration correctly:

```csharp
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    var config = builder.Configuration.GetSection("PrimusLogging");
    
    options.ApplicationId = config["ApplicationId"] ?? "APP";
    options.Environment = config["Environment"] ?? "development";
    options.MinLevel = (LogLevel)int.Parse(config["MinLevel"] ?? "1");
    
    var targetsSection = config.GetSection("Targets");
    options.Targets = targetsSection.Get<List<TargetConfig>>() ?? new List<TargetConfig>();
});
```

**Check:**
1. Section name is exactly `"PrimusLogging"` (case-sensitive)
2. JSON is valid (use a JSON validator)
3. Configuration file is being copied to output directory

### Issue: MinLevel Not Working

**Symptoms:** Logs appear even when MinLevel should filter them

**Cause:** Using string instead of integer in appsettings.json

❌ **Wrong:**
```json
{
  "MinLevel": "Information"
}
```

✅ **Correct:**
```json
{
  "MinLevel": 1
}
```

**Log Level Values:**
- `0` = Debug
- `1` = Info
- `2` = Warning
- `3` = Error
- `4` = Critical

---

## Logging Not Working

### Issue: No Logs Appearing

**Checklist:**

1. **Verify logging is configured:**
   ```csharp
   builder.Logging.AddPrimus(options => { ... });
   ```

2. **Check MinLevel is not too high:**
   ```csharp
   options.MinLevel = LogLevel.Debug;  // Allow all logs
   ```

3. **Verify target configuration:**
   ```csharp
   options.Targets = new List<TargetConfig>
   {
       new() { Type = "console", Pretty = true }
   };
   ```

4. **Check if default logging is interfering:**
   ```csharp
   builder.Logging.ClearProviders();  // Clear default providers first
   ```

5. **Verify you're using the logger:**
   ```csharp
   _logger.LogInformation("Test message");
   ```

### Issue: File Logs Not Created

**Symptoms:** Console logs work, but file logs don't appear

**Causes & Solutions:**

1. **Path not writable:**
   ```csharp
   // Ensure directory exists and is writable
   new TargetConfig
   {
       Type = "file",
       Path = "logs/app.log"  // Relative path creates in app directory
   }
   ```

2. **Async buffer not flushed:**
   - Logs are buffered when `Async = true`
   - Wait for application to flush on shutdown
   - Or disable async for testing:
   ```csharp
   new TargetConfig
   {
       Type = "file",
       Path = "logs/app.log",
       Async = false  // Immediate writes
   }
   ```

3. **File locked by another process:**
   - Close any text editors viewing the log file
   - Check for other instances of the application

---

## Middleware Issues

### Issue: "UsePrimusLogging" Not Found

**Symptoms:**
```
error CS1061: 'WebApplication' does not contain a definition for 'UsePrimusLogging'
```

**Cause:** Missing using directive

**Solution:** Add the correct namespace:

```csharp
using PrimusSaaS.Logging.Extensions;

var app = builder.Build();
app.UsePrimusLogging();
```

### Issue: Middleware Not Enriching Logs

**Symptoms:** HTTP context (request ID, user info) not in logs

**Checklist:**

1. **Middleware is added:**
   ```csharp
   app.UsePrimusLogging();
   ```

2. **Middleware is in correct order:**
   ```csharp
   var app = builder.Build();
   
   app.UseAuthentication();  // First
   app.UsePrimusLogging();   // After auth
   app.UseAuthorization();
   
   app.MapControllers();
   ```

3. **User context is set (if using custom auth):**
   ```csharp
   HttpContext.Items["PrimusUser"] = new Dictionary<string, object>
   {
       ["userId"] = "user-123",
       ["email"] = "user@example.com"
   };
   ```

### Issue: Request ID Not Appearing

**Cause:** Middleware not capturing request ID

**Solution:** The middleware auto-generates request IDs. To verify:

```csharp
// In a controller
[HttpGet]
public IActionResult Test()
{
    var requestId = HttpContext.Items["PrimusRequestId"];
    _logger.LogInformation("Request ID: {RequestId}", requestId);
    return Ok(new { requestId });
}
```

Check response headers for `X-Request-ID`.

---

## PII Masking Issues

### Issue: PII Not Being Masked

**Symptoms:** Emails, credit cards, SSNs appear in logs

**Checklist:**

1. **PII masking is enabled:**
   ```csharp
   options.Pii = new PiiOptions
   {
       MaskEmails = true,
       MaskCreditCards = true,
       MaskSSN = true
   };
   ```

2. **Custom keys are added:**
   ```csharp
   options.Pii.CustomSensitiveKeys.Add("password");
   options.Pii.CustomSensitiveKeys.Add("apiKey");
   ```

3. **Verify masking is working:**
   ```csharp
   _logger.LogInformation("User email: {Email}", "test@example.com");
   // Should log: "User email: ***REDACTED***"
   ```

### Issue: Custom Fields Not Masked

**Cause:** Key name doesn't match

**Solution:** Key matching is case-insensitive. Add exact key names:

```csharp
options.Pii.CustomSensitiveKeys = new List<string>
{
    "password",
    "apiKey",
    "secretKey",
    "token",
    "creditCard"
};
```

---

## Performance Issues

### Issue: Logging Slowing Down Application

**Symptoms:** High latency, slow response times

**Solutions:**

1. **Enable async file logging:**
   ```csharp
   new TargetConfig
   {
       Type = "file",
       Path = "logs/app.log",
       Async = true,
       BufferSize = 1000  // Adjust based on throughput
   }
   ```

2. **Increase MinLevel in production:**
   ```csharp
   options.MinLevel = LogLevel.Info;  // Skip Debug logs
   ```

3. **Reduce context data:**
   ```csharp
   // Instead of logging entire objects
   _logger.LogInformation("User: {@User}", largeUserObject);
   
   // Log only what's needed
   _logger.LogInformation("User {UserId} logged in", user.Id);
   ```

### Issue: Log Files Growing Too Large

**Symptoms:** Disk space exhaustion

**Solution:** Configure file rotation:

```csharp
new TargetConfig
{
    Type = "file",
    Path = "logs/app.log",
    MaxFileSize = 10 * 1024 * 1024,  // 10MB
    MaxRetainedFiles = 5,             // Keep only 5 old files
    CompressRotatedFiles = true       // Compress old files
}
```

---

## Integration Issues

### Issue: Integration with PrimusSaaS.Identity.Validator

**Symptoms:** User context not appearing in logs

**Solution:** The middleware automatically extracts user info from authenticated requests:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Identity Validator
builder.Services.AddPrimusIdentity(options => { ... });

// Add Logging
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options => { ... });

var app = builder.Build();

// Order matters!
app.UseAuthentication();      // 1. Authenticate first
app.UsePrimusLogging();       // 2. Then add logging middleware
app.UseAuthorization();       // 3. Then authorize

app.MapControllers();
app.Run();
```

The middleware will automatically extract:
- `userId` from `sub` or `userId` claim
- `email` from `email` claim

### Issue: Logs Not Appearing in Application Insights

**Symptoms:** Console/file logs work, but nothing in Azure

**Checklist:**

1. **Connection string is correct:**
   ```csharp
   new TargetConfig
   {
       Type = "applicationInsights",
       ConnectionString = "InstrumentationKey=your-actual-key"
   }
   ```

2. **Network connectivity:**
   - Ensure application can reach Azure
   - Check firewall rules

3. **Verify in Azure Portal:**
   - Logs may take 2-5 minutes to appear
   - Check "Logs" section in Application Insights

---

## Debugging Tips

### Enable Detailed Logging

Temporarily set MinLevel to Debug:

```csharp
options.MinLevel = LogLevel.Debug;
```

### Test with Simple Configuration

Start with minimal config and add features incrementally:

```csharp
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "TEST";
    options.Environment = "development";
    options.MinLevel = LogLevel.Debug;
    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = true }
    };
});
```

### Verify Logger Instance

Check that logger is injected correctly:

```csharp
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    
    public TestController(ILogger<TestController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("TestController created");
    }
}
```

---

## Getting Help

If you're still experiencing issues:

1. **Check the version:** Ensure you're using the latest version
   ```bash
   dotnet list package | grep PrimusSaaS.Logging
   ```

2. **Review documentation:**
   - [README.md](README.md)
   - [CONFIGURATION_GUIDE.md](CONFIGURATION_GUIDE.md)

3. **Create a minimal reproduction:**
   - Isolate the issue in a small test project
   - Share configuration and code

4. **Report the issue:**
   - GitHub Issues: https://github.com/primus-saas/logging/issues
   - Include: .NET version, package version, configuration, error messages
