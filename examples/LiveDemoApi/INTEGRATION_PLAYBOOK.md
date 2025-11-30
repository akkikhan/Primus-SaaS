# Primus SaaS Integration Playbook

> **Purpose**: A complete, battle-tested guide for integrating Primus SaaS packages (Identity, Logging, Notifications) into a new .NET application. This playbook captures real-world lessons learned, configuration pitfalls, and step-by-step instructions to ensure smooth integration without hitting the same issues we encountered during development.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Package Installation](#package-installation)
3. [Configuration Files](#configuration-files)
4. [Program.cs Setup](#programcs-setup)
5. [Demo Endpoints](#demo-endpoints)
6. [Common Pitfalls & Solutions](#common-pitfalls--solutions)
7. [Pre-Demo Verification Checklist](#pre-demo-verification-checklist)
8. [Environment-Specific Configuration](#environment-specific-configuration)

---

## Prerequisites

### Required Tools
- **.NET SDK 8.0** or higher
- **Visual Studio 2022** or **VS Code** with C# extension
- **Azure CLI** (for Azure AD token testing)
- **Postman** or similar API testing tool

### Required Accounts (for full demo)
- **Auth0 Account**: Client ID, Client Secret, Domain, Audience
- **Azure AD**: Tenant ID, Application (Client) ID
- **SMTP Provider**: Gmail App Password or similar
- **Twilio Account**: Account SID, Auth Token, Phone Number (trial or paid)

---

## Package Installation

### Step 1: Create New .NET Web API Project

```bash
dotnet new webapi -n YourDemoApp
cd YourDemoApp
```

### Step 2: Install Primus SaaS Packages

```bash
# Core packages
dotnet add package PrimusSaaS.Identity.Validator
dotnet add package PrimusSaaS.Logging
dotnet add package PrimusSaaS.Notifications

# Optional: Swagger for API documentation
dotnet add package Swashbuckle.AspNetCore
```

### Step 3: Verify Package Installation

```bash
dotnet restore
dotnet build
```

> **✅ Success Indicator**: Build completes without errors.

---

## Configuration Files

### appsettings.json Structure

Create or update `appsettings.json` with the following structure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "PrimusLogging": {
    "ApplicationId": "YourDemoApp",
    "MinimumLevel": "Information",
    "RedactSensitiveData": true,
    "EnableScopes": true,
    "Targets": {
      "Console": {
        "Enabled": true
      },
      "File": {
        "Enabled": true,
        "Path": "logs/app-.log",
        "RollingInterval": "Day"
      }
    }
  },
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "Auth0",
        "Type": "Oidc",
        "Issuer": "https://YOUR_AUTH0_DOMAIN.auth0.com/",
        "Authority": "https://YOUR_AUTH0_DOMAIN.auth0.com/",
        "Audiences": [
          "https://your-api-audience"
        ],
        "AllowMachineToMachine": true,
        "AllowedGrantTypes": [
          "client_credentials",
          "client-credentials"
        ]
      },
      {
        "Name": "AzureAD",
        "Type": "Oidc",
        "Issuer": "https://sts.windows.net/YOUR_TENANT_ID/",
        "Authority": "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0",
        "Audiences": [
          "YOUR_CLIENT_ID",
          "api://YOUR_CLIENT_ID"
        ]
      }
    ],
    "RequireHttpsMetadata": false
  },
  "Notifications": {
    "Smtp": {
      "Host": "",
      "Port": 587,
      "Username": "",
      "Password": "",
      "EnableSsl": true,
      "FromAddress": "",
      "FromName": "Demo App"
    },
    "Twilio": {
      "AccountSid": "",
      "AuthToken": "",
      "FromNumber": "",
      "MessagingServiceSid": "",
      "ValidateOnStartup": false
    }
  },
  "AllowedHosts": "*"
}
```

### appsettings.Development.json

Override with real credentials for local testing:

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Debug"
  },
  "PrimusIdentity": {
    "RequireHttpsMetadata": false
  },
  "Notifications": {
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "Username": "your-email@gmail.com",
      "Password": "your-app-password",
      "EnableSsl": true,
      "FromAddress": "your-email@gmail.com",
      "FromName": "Demo App"
    },
    "Twilio": {
      "AccountSid": "ACxxxxxxxxxxxxxxxxxxxx",
      "AuthToken": "your-auth-token",
      "FromNumber": "+1234567890",
      "ValidateOnStartup": false
    }
  }
}
```

> **⚠️ IMPORTANT**: Never commit `appsettings.Development.json` with real credentials. Add it to `.gitignore`.

---

## Program.cs Setup

### Complete Program.cs Template

```csharp
using Microsoft.AspNetCore.Authorization;
using Primus.Identity.Validator;
using Primus.Logging;
using Primus.Notifications.Core;
using Primus.Notifications.Email;
using Primus.Notifications.Sms;
using System.Diagnostics;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. LOGGING CONFIGURATION
// ============================================================================
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(options);
});

// ============================================================================
// 2. IDENTITY CONFIGURATION
// ============================================================================
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

// ============================================================================
// 3. NOTIFICATIONS CONFIGURATION
// ============================================================================
var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddPrimusNotifications(options =>
{
    // Template provider
    options.UseFileTemplates(
        templatesPath,
        validateOnStartup: true,
        watchForChanges: isDevelopment
    );

    // Always use logger for fallback
    options.UseLogger();

    // In-memory queue for retry logic
    options.UseInMemoryQueue();

    // SMTP configuration (if credentials present)
    var smtpConfig = builder.Configuration.GetSection("Notifications:Smtp");
    if (!string.IsNullOrEmpty(smtpConfig["Host"]) &&
        !string.IsNullOrEmpty(smtpConfig["Username"]))
    {
        options.UseSmtp(smtp =>
        {
            smtpConfig.Bind(smtp);
        });
    }

    // Twilio configuration (conditional)
    var twilioConfig = builder.Configuration.GetSection("Notifications:Twilio");
    if (!string.IsNullOrEmpty(twilioConfig["AccountSid"]))
    {
        options.UseTwilio(
            builder.Configuration,
            "Notifications:Twilio",
            validateOnStartup: false
        );
    }
    else
    {
        // Fallback to logger-based SMS in dev
        options.UseSms();
    }

    // Dispatch options (surface errors during demo)
    options.DispatchOptions.ThrowOnFailure = true;
    options.DispatchOptions.FallbackToLogger = false;
});

// ============================================================================
// 4. CORS CONFIGURATION
// ============================================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",  // Vite default
            "http://localhost:3000"   // React default
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// ============================================================================
// 5. STANDARD ASP.NET SERVICES
// ============================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ============================================================================
// 6. MIDDLEWARE PIPELINE
// ============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ============================================================================
// 7. DEMO ENDPOINTS
// ============================================================================

// Primus Identity Diagnostics
app.MapPrimusIdentityDiagnostics();

// Health check for notifications
app.MapGet("/notifications/health", (INotificationService notificationService) =>
{
    var channels = notificationService.GetAvailableChannels();
    return Results.Ok(new
    {
        timestamp = DateTime.UtcNow,
        channels = channels.Select(c => new
        {
            type = c.ToString(),
            status = "available"
        })
    });
});

// Send welcome email
app.MapPost("/notifications/welcome", async (
    INotificationService notificationService,
    [FromBody] EmailRequest request) =>
{
    var notification = new BasicNotification
    {
        Type = "Welcome",
        Recipients = new[] { request.Email },
        Data = new Dictionary<string, object>
        {
            ["UserName"] = request.Name ?? "User",
            ["AppName"] = "Demo App"
        }
    };

    var result = await notificationService.SendAsync(notification);
    return result.Success
        ? Results.Ok(new { message = "Email sent successfully", result })
        : Results.BadRequest(new { message = "Failed to send email", result });
});

// Send SMS
app.MapPost("/notifications/sms", async (
    INotificationService notificationService,
    [FromBody] SmsRequest request) =>
{
    var notification = new BasicNotification
    {
        Type = "SMS",
        Recipients = new[] { request.PhoneNumber },
        Data = new Dictionary<string, object>
        {
            ["Message"] = request.Message
        }
    };

    var result = await notificationService.SendAsync(notification);
    return result.Success
        ? Results.Ok(new { message = "SMS sent successfully", result })
        : Results.BadRequest(new { message = "Failed to send SMS", result });
});

// Auth0 token proxy
app.MapPost("/auth/auth0", async (HttpContext context) =>
{
    var body = await JsonSerializer.DeserializeAsync<Auth0TokenRequest>(context.Request.Body);
    
    using var client = new HttpClient();
    var tokenRequest = new
    {
        client_id = body.ClientId,
        client_secret = body.ClientSecret,
        audience = body.Audience,
        grant_type = "client_credentials"
    };

    var response = await client.PostAsJsonAsync(
        $"https://{body.Domain}/oauth/token",
        tokenRequest
    );

    var result = await response.Content.ReadAsStringAsync();
    return Results.Content(result, "application/json");
});

// Azure AD token via CLI
app.MapPost("/auth/azure", async (HttpContext context) =>
{
    var body = await JsonSerializer.DeserializeAsync<AzureTokenRequest>(context.Request.Body);
    
    var psi = new ProcessStartInfo
    {
        FileName = "az",
        Arguments = $"account get-access-token --resource {body.Resource}",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(psi);
    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();

    return Results.Content(output, "application/json");
});

// Secured endpoint (requires valid JWT)
app.MapGet("/weatherforecast", [Authorize] () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" }[Random.Shared.Next(10)]
        ))
        .ToArray();
    return forecast;
});

app.Run();

// ============================================================================
// 8. REQUEST/RESPONSE MODELS
// ============================================================================

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record EmailRequest(string Email, string? Name);
record SmsRequest(string PhoneNumber, string Message);
record Auth0TokenRequest(string Domain, string ClientId, string ClientSecret, string Audience);
record AzureTokenRequest(string Resource);
```

---

## Demo Endpoints

### Create Templates Directory

Create `Templates/Welcome.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #4CAF50; color: white; padding: 20px; text-align: center; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Welcome to {{AppName}}!</h1>
        </div>
        <p>Hello {{UserName}},</p>
        <p>Thank you for joining us. We're excited to have you on board!</p>
        <p>Best regards,<br>The {{AppName}} Team</p>
    </div>
</body>
</html>
```

Create `Templates/Welcome.txt`:

```
Hello {{UserName}},

Welcome to {{AppName}}!

Thank you for joining us. We're excited to have you on board!

Best regards,
The {{AppName}} Team
```

---

## Common Pitfalls & Solutions

### 🔴 Issue 1: Port Already in Use

**Symptom**: `System.IO.IOException: Failed to bind to address http://localhost:5221: address already in use`

**Root Cause**: Previous instance of the app is still running.

**Solution**:
```bash
# Find and kill the process
netstat -ano | findstr :5221
taskkill /PID <PID> /F

# Or change the port in launchSettings.json
```

---

### 🔴 Issue 2: Twilio "To Number Not Verified" (Error 21608)

**Symptom**: SMS fails with "The number +91XXXXXXXXXX is unverified. Trial accounts cannot send messages to unverified numbers"

**Root Cause**: Twilio trial accounts can only send to verified numbers.

**Solutions**:
1. **Verify the destination number** in Twilio Console → Phone Numbers → Verified Caller IDs
2. **Upgrade to paid account** for unrestricted sending
3. **Disable Twilio in dev** and use logger-based SMS:
   ```json
   "Notifications": {
     "Twilio": {
       "AccountSid": "",  // Leave empty to disable
       ...
     }
   }
   ```

---

### 🔴 Issue 3: Twilio "From Number Cannot Equal To Number" (Error 21266)

**Symptom**: SMS fails when testing with the same number as sender and recipient.

**Solution**: Always use a different destination number than your Twilio sender number.

---

### 🔴 Issue 4: Twilio Geographic Permissions

**Symptom**: Cannot send SMS to international numbers (e.g., India from US number).

**Root Cause**: US long codes are domestic-only by default.

**Solutions**:
1. **Enable geographic permissions** in Twilio Console → Messaging → Settings → Geo Permissions
2. **Use a Messaging Service** with international capabilities
3. **Purchase a local number** in the target country

---

### 🔴 Issue 5: Auth0 "Invalid Grant Type"

**Symptom**: Token request fails with `"error": "unauthorized_client", "error_description": "Grant type 'client_credentials' not allowed"`

**Root Cause**: Application not configured for machine-to-machine (M2M) authentication.

**Solution**:
1. In Auth0 Dashboard → Applications → Your App → Settings
2. Set **Application Type** to "Machine to Machine"
3. Under **Advanced Settings → Grant Types**, enable `client_credentials`
4. In `appsettings.json`:
   ```json
   "AllowMachineToMachine": true,
   "AllowedGrantTypes": ["client_credentials", "client-credentials"]
   ```

---

### 🔴 Issue 6: Azure AD Token Validation Fails

**Symptom**: Valid Azure token rejected with "IDX10205: Issuer validation failed"

**Root Cause**: Mismatch between token issuer (V1: `sts.windows.net`) and configured issuer (V2: `login.microsoftonline.com`).

**Solution**: Use V1 issuer in configuration:
```json
"Issuer": "https://sts.windows.net/YOUR_TENANT_ID/",
"Authority": "https://login.microsoftonline.com/YOUR_TENANT_ID/v2.0"
```

---

### 🔴 Issue 7: HTTPS Redirection Warning in Development

**Symptom**: Browser shows "Your connection is not private" when testing locally.

**Solution**:
1. **Disable HTTPS redirection** in `Program.cs` for local dev:
   ```csharp
   if (!app.Environment.IsDevelopment())
   {
       app.UseHttpsRedirection();
   }
   ```
2. **Or trust the dev certificate**:
   ```bash
   dotnet dev-certs https --trust
   ```

---

### 🔴 Issue 8: Notification Errors Silently Swallowed

**Symptom**: SMS/Email fails but API returns success.

**Root Cause**: Default `FallbackToLogger = true` masks provider failures.

**Solution**: Set strict dispatch options for demos:
```csharp
options.DispatchOptions.ThrowOnFailure = true;
options.DispatchOptions.FallbackToLogger = false;
```

---

### 🔴 Issue 9: Template Not Found

**Symptom**: `TemplateNotFoundException: Template 'Welcome' not found`

**Root Cause**: Templates directory not created or incorrect path.

**Solution**:
1. Create `Templates/` folder in project root
2. Add `Welcome.html` and `Welcome.txt`
3. Verify path in `Program.cs`:
   ```csharp
   var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
   ```
4. Ensure templates are copied to output:
   ```xml
   <ItemGroup>
     <None Update="Templates\**\*">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </None>
   </ItemGroup>
   ```

---

## Pre-Demo Verification Checklist

### ✅ Before Starting the Demo

- [ ] **Stop all running instances** of the app (check Task Manager)
- [ ] **Clean and rebuild** the project (`dotnet clean && dotnet build`)
- [ ] **Verify appsettings.Development.json** has real credentials
- [ ] **Check Templates folder** exists with `Welcome.html` and `Welcome.txt`
- [ ] **Test SMTP credentials** (try sending test email via Gmail)
- [ ] **Verify Twilio destination number** (must be verified for trial accounts)
- [ ] **Confirm Auth0 configuration** (client ID, secret, audience, grant types)
- [ ] **Test Azure CLI** (`az login` and `az account get-access-token --resource https://management.azure.com/`)

### ✅ Startup Verification (Run `dotnet run`)

1. **Check console output** for startup logs:
   ```
   ✅ Primus Logging initialized
   ✅ Primus Identity configured with 2 issuers
   ✅ Primus Notifications configured with 3 channels
   ```

2. **Test `/notifications/health`**:
   ```bash
   curl http://localhost:5221/notifications/health
   ```
   Expected response:
   ```json
   {
     "timestamp": "2025-11-30T...",
     "channels": [
       { "type": "Email", "status": "available" },
       { "type": "Sms", "status": "available" },
       { "type": "Logger", "status": "available" }
     ]
   }
   ```

3. **Test `/primus/diagnostics`**:
   ```bash
   curl http://localhost:5221/primus/diagnostics
   ```
   Verify issuers and audiences are loaded correctly.

### ✅ Functional Tests

1. **Send Test Email**:
   ```bash
   curl -X POST http://localhost:5221/notifications/welcome \
     -H "Content-Type: application/json" \
     -d '{"email":"your-email@gmail.com","name":"Test User"}'
   ```
   Check inbox for welcome email.

2. **Send Test SMS** (use verified number for trial):
   ```bash
   curl -X POST http://localhost:5221/notifications/sms \
     -H "Content-Type: application/json" \
     -d '{"phoneNumber":"+1234567890","message":"Test from Demo App"}'
   ```
   Check phone for SMS.

3. **Test Auth0 Token**:
   ```bash
   curl -X POST http://localhost:5221/auth/auth0 \
     -H "Content-Type: application/json" \
     -d '{
       "domain":"YOUR_DOMAIN.auth0.com",
       "clientId":"YOUR_CLIENT_ID",
       "clientSecret":"YOUR_CLIENT_SECRET",
       "audience":"https://your-api-audience"
     }'
   ```
   Copy `access_token` from response.

4. **Test Secured Endpoint**:
   ```bash
   curl http://localhost:5221/weatherforecast \
     -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
   ```
   Should return weather forecast data.

---

## Environment-Specific Configuration

### Development
- **HTTPS**: Disabled or use trusted dev cert
- **Logging**: Debug level, console + file
- **Notifications**: Real SMTP/Twilio with test credentials
- **Identity**: `RequireHttpsMetadata = false`
- **Validation**: `ValidateOnStartup = false` for Twilio

### Staging
- **HTTPS**: Enabled
- **Logging**: Information level, file only
- **Notifications**: Production SMTP/Twilio
- **Identity**: `RequireHttpsMetadata = true`
- **Validation**: `ValidateOnStartup = true`

### Production
- **HTTPS**: Enforced
- **Logging**: Warning level, structured JSON to centralized logging
- **Notifications**: Production providers with monitoring
- **Identity**: `RequireHttpsMetadata = true`, strict validation
- **Secrets**: Use Azure Key Vault or similar, never appsettings

---

## Quick Start Commands

```bash
# 1. Create new project
dotnet new webapi -n MyDemoApp
cd MyDemoApp

# 2. Install packages
dotnet add package PrimusSaaS.Identity.Validator
dotnet add package PrimusSaaS.Logging
dotnet add package PrimusSaaS.Notifications

# 3. Copy configuration from this playbook
# - Update appsettings.json
# - Replace Program.cs
# - Create Templates folder

# 4. Build and run
dotnet build
dotnet run

# 5. Test health endpoint
curl http://localhost:5221/notifications/health
```

---

## Support & Troubleshooting

### Logs Location
- **Console**: Real-time output in terminal
- **File**: `logs/app-YYYYMMDD.log`

### Common Commands
```bash
# View real-time logs
tail -f logs/app-$(date +%Y%m%d).log

# Check running processes
netstat -ano | findstr :5221

# Test SMTP connection
telnet smtp.gmail.com 587

# Verify Azure CLI
az account show
```

### Getting Help
1. Check logs in `logs/` directory
2. Review this playbook's "Common Pitfalls" section
3. Verify configuration against templates
4. Test each component independently (logging → identity → notifications)

---

## Summary

This playbook provides everything needed to:
1. ✅ Install Primus SaaS packages in a new .NET app
2. ✅ Configure logging, identity, and notifications
3. ✅ Avoid common integration pitfalls
4. ✅ Verify functionality before demos
5. ✅ Troubleshoot issues quickly

**Key Takeaway**: Follow this playbook step-by-step, verify each component independently, and test thoroughly before presenting to senior management. The configurations here are battle-tested and production-ready.
