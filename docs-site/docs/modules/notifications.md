---
id: notifications
title: Notifications Module
sidebar_position: 3
description: Production-ready email and SMS notifications with Liquid templates, multiple providers, and async delivery.
---

# Notifications Module

## 1. Module Overview

The **Primus Notifications Module** is a production-ready notification delivery system that supports email (SMTP), SMS (Twilio, AWS SNS, Azure Communication Services), file-based Liquid templates, async queuing with background workers, and comprehensive instrumentation.

**Key benefits:**
- **Multi-channel delivery**: Email via SMTP, SMS via Twilio/AWS SNS/Azure Communication Services
- **Template engine**: Liquid templates with caching, validation, and subject/body file conventions
- **Async processing**: In-memory bounded queue with background worker for reliable delivery
- **Built-in diagnostics**: Health checks, metrics, and runtime statistics
- **Fail-safe design**: `NotificationResult` reports per-channel outcomes with optional throw-on-failure

---

## 2. Installation

### NuGet Package

```bash
dotnet add package PrimusSaaS.Notifications
```

**Current Version**: `1.4.2` (supports .NET 6, 7, and 8)

See [Modules Version Matrix](/docs/modules/version-matrix) for the authoritative version list.

> Heads-up: the package currently includes all email/SMS providers (SMTP, SendGrid, Twilio, AWS SES/SNS, Azure Communication Services) and their dependencies. Provider-specific package split is planned; for now, keep only the providers you configure at runtime.

### Starting from scratch
- New project: `dotnet new webapi -n MyPrimusNotifications --no-https`
- Add package: `cd MyPrimusNotifications && dotnet add package PrimusSaaS.Notifications`
- Swagger (if missing): `dotnet add package Swashbuckle.AspNetCore`
- Run: `dotnet run --urls=http://localhost:5004`

---

## 3. Required Using Statements

Add these using statements to your `Program.cs` or relevant files:

```csharp
// Core notifications
using PrimusSaaS.Notifications;

// For INotificationService injection
using PrimusSaaS.Notifications.Abstractions;

// For configuration options
using PrimusSaaS.Notifications.Configuration;

// For custom notification types
using PrimusSaaS.Notifications.Core;

// For health service (optional)
using PrimusSaaS.Notifications.Services;
```

---

## 4. Program.cs Service Registration
Register notifications services/providers so you can send email/SMS with templates, queues, and logger fallback.

### Quick Start (Simplest)

```csharp
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseLogger(); // dev-safe: logs instead of sending
    notifications.UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"));
    notifications.UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates"));
});

var app = builder.Build();

app.MapPost("/send-email", async (INotificationService notifications) =>
{
    var result = await notifications.SendEmailAsync(
        "recipient@example.com",
        "Hello!",
        "<p>This is a test email.</p>");
    return result.Success ? Results.Ok() : Results.Problem(result.FailureReason);
});

app.Run();
```

### Option B: Advanced (templates + queue + SMTP + Twilio)

```csharp
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;

var builder = WebApplication.CreateBuilder(args);
var templatesRoot = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");

builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseFileTemplates(
        templatesRoot,
        validateOnStartup: true,
        watchForChanges: builder.Environment.IsDevelopment());

    notifications.UseLogger();
    notifications.UseInMemoryQueue(o =>
    {
        o.BoundedCapacity = 500;
        o.MaxParallelHandlers = 2;
        o.BaseRetryDelayMs = 250;
    });
    notifications.UseSmtp(opts => builder.Configuration.GetSection("Notifications:Smtp").Bind(opts));
    notifications.UseTwilio(opts => builder.Configuration.GetSection("Notifications:Twilio").Bind(opts));
    notifications.ConfigureDispatch(o =>
    {
        o.ThrowOnFailure = true;
        o.FallbackToLogger = false;
        o.QueueOnFailure = false;
    });
});

var app = builder.Build();
app.MapControllers();
app.Run();
```

### Option C: Development Mode (Logger Only)

```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications
        .UseLogger()  // Logs all notifications to ILogger
        .UseSms();    // Logs SMS payloads (no actual sending)
});
```

### Get your keys (notifications)
- **SMTP**: From your provider (e.g., SendGrid/Exchange/SES/Office365), collect host, port, username, password, From address/name, SSL. Store secrets in User Secrets/Key Vault.
- **Twilio SMS**: Twilio Console → Account Info → Account SID/Auth Token; Messaging → number → FromNumber (E.164).
- **Templates**: Create `NotificationTemplates/<TemplateName>/EmailSubject.liquid` and `EmailBody.liquid` (or SMS equivalents).

---

## 5. Configuration (appsettings.json)
Provide SMTP/Twilio settings and queue options so channels can send and retry correctly.

### Full Configuration Example

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "your-username",
      "Password": "use-user-secrets-not-here",
      "FromAddress": "no-reply@example.com",
      "FromName": "My App",
      "EnableSsl": true,
      "MaxRetryCount": 2,
      "RetryBaseDelayMs": 200,
      "TimeoutSeconds": 30
    },
    "Twilio": {
      "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxx",
      "AuthToken": "use-user-secrets-not-here",
      "FromNumber": "+15551234567"
    },
    "NotificationQueue": {
      "BoundedCapacity": 500,
      "MaxParallelHandlers": 2,
      "BaseRetryDelayMs": 250
    }
  }
}
```

> For AWS SNS or Azure Communication Services SMS, see the advanced providers section (optional; not required for SMTP/Twilio flows).

### Other SMS Providers (optional)
If you prefer AWS SNS or Azure Communication Services instead of Twilio:
- Pick one provider (only one SMS channel is active at a time).
- Add the provider package/credentials in `Notifications:*` config.
- Swap `UseTwilio(...)` for the provider call below.

<details>
<summary>Use AWS SNS (SMS)</summary>

```csharp
notifications.UseAwsSns(opts =>
    builder.Configuration.GetSection("Notifications:AwsSns").Bind(opts));
```

```json
"Notifications": {
  "AwsSns": {
    "AccessKeyId": "AKIA...",
    "SecretAccessKey": "use-user-secrets",
    "Region": "us-east-1",
    "SmsType": "Transactional",
    "SenderId": "MyApp"
  }
}
```

Docs: https://docs.aws.amazon.com/sns/latest/dg/sms_publish-to-phone.html
</details>

<details>
<summary>Use Azure Communication Services (SMS)</summary>

```csharp
notifications.UseAzureCommunicationServices(opts =>
    builder.Configuration.GetSection("Notifications:AzureCommunicationServices").Bind(opts));
```

```json
"Notifications": {
  "AzureCommunicationServices": {
    "ConnectionString": "endpoint=https://<resource>.communication.azure.com/;accesskey=...",
    "FromNumber": "+18001234567",
    "EnableDeliveryReport": true,
    "Tag": "my-app"
  }
}
```

Docs: https://learn.microsoft.com/azure/communication-services/quickstarts/sms/send
</details>

---

## Examples and downloads

- **Minimal**: `examples/notifications/Minimal` — [Download zip](/downloads/notifications-minimal.zip) — Postman included in the zip.
- **Advanced**: `examples/notifications/Advanced` — [Download zip](/downloads/notifications-advanced.zip) — Postman included in the zip.
- Swagger (static): [Minimal](/downloads/notifications-minimal-swagger.json), [Advanced](/downloads/notifications-advanced-swagger.json)
- Full-stack reference: `examples/LiveDemoApi` (notifications + other modules).
- Verified with:
  - Minimal: `cd examples/notifications/Minimal && dotnet restore && dotnet run --urls=http://localhost:5004`
  - Advanced: `cd examples/notifications/Advanced && dotnet restore && dotnet run --urls=http://localhost:5005`
- Quick curl:
  - Welcome email: `curl -X POST "http://localhost:5004/notify/welcome?email=test@example.com"`
  - SMS ping (advanced): `curl -X POST "http://localhost:5005/notify/sms?number=+15551234567"`

> Provider-specific sections (e.g., AWS SNS, Azure Communication Services) are optional and can be skimmed; core flow is SMTP + Twilio + logger + queue/dispatch.

### SMTP Options Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Host` | string | **required** | SMTP server hostname |
| `Port` | int | `587` | SMTP port (587 for TLS, 465 for SSL) |
| `Username` | string | `""` | SMTP authentication username |
| `Password` | string | `""` | SMTP authentication password |
| `FromAddress` | string | **required** | Sender email address |
| `FromName` | string | `""` | Sender display name |
| `EnableSsl` | bool | `true` | Enable TLS/SSL |
| `TimeoutSeconds` | int | `30` | Connection timeout |
| `MaxRetryCount` | int | `2` | Maximum retry attempts |
| `RetryBaseDelayMs` | int | `200` | Base delay for exponential backoff |

### SMS Provider Comparison

| Provider | Best For | Key Features |
|----------|----------|--------------|
| **Twilio** | Most use cases | Widest carrier support, excellent docs |
| **AWS SNS** | AWS-heavy stack | Pay-per-use, great for transactional SMS |
| **Azure** | Azure-heavy stack | Native Azure integration, combined billing |

---

## 6. Template Directory Structure

### Folder Layout

```text
NotificationTemplates/
├── PasswordReset/
│   ├── EmailSubject.liquid    # Subject line for email
│   ├── EmailBody.liquid       # HTML body for email
│   └── SmsBody.liquid         # SMS text content
├── WelcomeEmail/
│   ├── EmailSubject.liquid
│   └── EmailBody.liquid
├── OrderConfirmation/
│   ├── EmailSubject.liquid
│   ├── EmailBody.liquid
│   └── SmsBody.liquid
└── Partials/
    ├── email_header.liquid
    └── email_footer.liquid
```

### Template Variables

| Variable | Description |
|----------|-------------|
| `{{ recipient.email }}` | Recipient email address |
| `{{ recipient.name }}` | Recipient name (may be null) |
| `{{ app.name }}` | Application name |
| `{{ app.url }}` | Application base URL |
| `{{ data.* }}` | Custom data fields from notification |

### Example Templates

**`PasswordReset/EmailSubject.liquid`**:
```liquid
Reset Your {{ app.name }} Password
```

**`PasswordReset/EmailBody.liquid`**:
```liquid
{% include 'Partials/email_header' %}

{% if recipient.name %}
<p>Hi {{ recipient.name }},</p>
{% else %}
<p>Hi there,</p>
{% endif %}

<p>You requested a password reset. Click the link below to set a new password:</p>

<p><a href="{{ data.link }}">Reset Password</a></p>

<p>This link expires in {{ data.expiryMinutes }} minutes.</p>

<p>If you didn't request this, please ignore this email.</p>

{% include 'Partials/email_footer' %}
```

**`PasswordReset/SmsBody.liquid`**:
```liquid
Your {{ app.name }} password reset code is {{ data.code }}. Expires in {{ data.expiryMinutes }} minutes.
```

---

## 7. Required Dependencies

The package automatically includes these dependencies:

| Package | Version | Purpose |
|---------|---------|---------|
| `Fluid.Core` | 2.5.0+ | Liquid template engine |
| `Microsoft.Extensions.DependencyInjection` | 6.0.0+ | DI abstractions |
| `Microsoft.Extensions.Logging` | 6.0.0+ | Logging abstractions |

### Optional Provider Dependencies

These are included but only activated when configured:

| Provider | Package | When Activated |
|----------|---------|----------------|
| Twilio | `Twilio` | When `UseTwilio()` is called |
| AWS SNS | `AWSSDK.SimpleNotificationService` | When `UseAwsSns()` is called |
| Azure SMS | `Azure.Communication.Sms` | When `UseAzureCommunicationServices()` is called |

---

## 8. External Guides & Resources

### SMTP Setup
- [Gmail SMTP Settings](https://support.google.com/mail/answer/7126229)
- [SendGrid SMTP Integration](https://docs.sendgrid.com/for-developers/sending-email/integrating-with-the-smtp-api)
- [Amazon SES SMTP](https://docs.aws.amazon.com/ses/latest/dg/send-email-smtp.html)

### SMS Providers
- [Twilio Getting Started](https://www.twilio.com/docs/sms/quickstart/csharp)
- [AWS SNS SMS](https://docs.aws.amazon.com/sns/latest/dg/sms_publish-to-phone.html)
- [Azure Communication Services SMS](https://learn.microsoft.com/azure/communication-services/quickstarts/sms/send)

### Liquid Templates
- [Liquid Template Language](https://shopify.github.io/liquid/)
- [Fluid (C# Liquid Implementation)](https://github.com/sebastienros/fluid)

---

## 9. End-to-End Working Example

### Complete Minimal API Example

**Step 1: Create project and install package**
```bash
dotnet new webapi -n MyNotificationsApi
cd MyNotificationsApi
dotnet add package PrimusSaaS.Notifications
```

**Step 2: Create template directory structure**
```bash
mkdir -p NotificationTemplates/Welcome
```

**Step 3: Create `NotificationTemplates/Welcome/EmailSubject.liquid`**
```liquid
Welcome to {{ app.name }}, {{ recipient.name }}!
```

**Step 4: Create `NotificationTemplates/Welcome/EmailBody.liquid`**
```html
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .button { background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>Welcome, {{ recipient.name }}!</h1>
        <p>Thanks for joining {{ app.name }}. We're excited to have you.</p>
        <p><a class="button" href="{{ data.dashboardUrl }}">Go to Dashboard</a></p>
    </div>
</body>
</html>
```

**Step 5: Replace `Program.cs`**
```csharp
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;
using PrimusSaaS.Notifications.Services;

var builder = WebApplication.CreateBuilder(args);
var templatesRoot = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");

builder.Services.AddPrimusNotifications(notifications =>
{
    notifications
        .UseFileTemplates(templatesRoot, validateOnStartup: true)
        .UseLogger();  // Logs notifications in development
});

var app = builder.Build();

// Send a simple email (no template)
app.MapPost("/send-simple", async (INotificationService notifications) =>
{
    var result = await notifications.SendEmailAsync(
        "test@example.com",
        "Test Subject",
        "<p>This is a test email.</p>");
    
    return result.Success 
        ? Results.Ok(new { Message = "Email sent!" })
        : Results.Problem(result.FailureReason);
});

// Send a templated notification
app.MapPost("/send-welcome", async (
    WelcomeRequest request,
    INotificationService notifications) =>
{
    var notification = new WelcomeNotification(request.Email, request.Name);
    var result = await notifications.SendAsync(notification);
    
    return result.Success 
        ? Results.Ok(new { Message = "Welcome email sent!" })
        : Results.Problem(result.FailureReason);
});

// Health check endpoint
app.MapGet("/health/notifications", async (NotificationHealthService health) =>
{
    var snapshot = await health.GetChannelHealthAsync();
    return Results.Json(snapshot);
});

app.Run();

// Request model
record WelcomeRequest(string Email, string Name);

// Custom notification type
public record WelcomeNotification(string Email, string Name) : INotification
{
    public string Type => "Welcome";
    public object Data => new 
    { 
        DashboardUrl = "https://example.com/dashboard" 
    };
    public IEnumerable<string> Channels => new[] { "Email", "Logger" };
    public Recipient Recipient => new() { Email = Email, Name = Name };
}
```

**Step 6: Run and test**
```bash
dotnet run
```

**Step 7: Test with curl**
```bash
# Send simple email
curl -X POST http://localhost:5000/send-simple

# Send templated welcome email
curl -X POST http://localhost:5000/send-welcome \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "name": "John"}'

# Check health
curl http://localhost:5000/health/notifications
```

### Controller-Based Example

```csharp
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Notifications.Abstractions;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly INotificationQueue _queue;

    public NotificationsController(
        INotificationService notifications,
        INotificationQueue queue)
    {
        _notifications = notifications;
        _queue = queue;
    }

    [HttpPost("password-reset")]
    public async Task<IActionResult> SendPasswordReset([FromBody] PasswordResetRequest request)
    {
        var notification = new PasswordResetNotification(
            request.Email,
            request.Name,
            request.ResetCode,
            request.ResetLink);

        // Queue for async delivery (recommended for better response times)
        await _queue.EnqueueAsync(notification);
        
        return Accepted(new { Message = "Password reset email queued" });
    }

    [HttpPost("send-immediate")]
    public async Task<IActionResult> SendImmediate([FromBody] SendRequest request)
    {
        var result = await _notifications.SendEmailAsync(
            request.To,
            request.Subject,
            request.Body);

        if (!result.Success)
        {
            return StatusCode(500, new { Error = result.FailureReason });
        }

        return Ok(new { Message = "Email sent", Channel = result.ChannelUsed });
    }
}

public record PasswordResetRequest(string Email, string Name, string ResetCode, string ResetLink);
public record SendRequest(string To, string Subject, string Body);

public record PasswordResetNotification(
    string Email, 
    string Name, 
    string Code, 
    string Link) : INotification
{
    public string Type => "PasswordReset";
    public object Data => new { Code, Link, ExpiryMinutes = 15 };
    public IEnumerable<string> Channels => new[] { "Email", "Sms" };
    public Recipient Recipient => new() { Email = Email, Name = Name };
}
```

---

## 10. Troubleshooting

### Common Issues and Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| `NotificationFailedException` | No channel succeeded | Check SMTP credentials and connectivity |
| `ServiceUnavailable` in result | SMS provider not configured | Configure Twilio/AWS SNS/Azure credentials |
| Template not found | Wrong path or naming | Verify folder structure: `{Type}/EmailBody.liquid` |
| Liquid syntax error | Invalid template | Enable `validateOnStartup: true` to catch early |
| Email not delivered | SMTP authentication | Check credentials, enable app passwords if needed |

### Debug Mode

Add logging to see notification flow:

```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseLogger();  // Always logs notifications
    notifications.UseSmtp(builder.Configuration.GetSection("Smtp"));
});
```

### Verify SMTP Connection

```csharp
app.MapGet("/test-smtp", async (INotificationService notifications) =>
{
    try
    {
        var result = await notifications.SendEmailAsync(
            "your-email@example.com",
            "SMTP Test",
            "<p>If you receive this, SMTP is working!</p>");
        return Results.Json(new 
        { 
            Success = result.Success, 
            Channel = result.ChannelUsed,
            Channels = result.Channels 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});
```

### Check Channel Health

```csharp
app.MapGet("/health/notifications", async (NotificationHealthService health) =>
{
    var snapshot = await health.GetChannelHealthAsync();
    return Results.Json(snapshot);
});
```

Returns:
```json
{
  "email": { "configured": true, "status": "healthy" },
  "sms": { "configured": false, "status": "unconfigured" },
  "logger": { "configured": true, "status": "healthy" }
}
```

---

## 11. FAQ

### Q: Can I send notifications without templates?
**A:** Yes! Use the direct helpers:
```csharp
await notifications.SendEmailAsync("to@example.com", "Subject", "<p>Body</p>");
await notifications.SendSmsAsync("+15551234567", "Your code is 123456");
```

### Q: How do I switch SMS providers?
**A:** Replace the `UseTwilio()` call with `UseAwsSns()` or `UseAzureCommunicationServices()`. Only one SMS provider can be active at a time.

### Q: Does the queue persist across restarts?
**A:** No, the in-memory queue is lost on restart. For durability, implement a custom `INotificationQueue` backed by a database or message broker.

### Q: How do I send both email and SMS for the same notification?
**A:** Include both channels in your notification:
```csharp
public IEnumerable<string> Channels => new[] { "Email", "Sms" };
```

### Q: What happens if one channel fails?
**A:** By default, success is reported if at least one channel succeeds. Check `result.Channels` for per-channel status.

### Q: How do I add custom data to templates?
**A:** Add properties to the `Data` object in your notification:
```csharp
public object Data => new { OrderId, Total, ItemCount, TrackingUrl };
```
Access in template: `{{ data.orderId }}`, `{{ data.total }}`, etc.

---

## 12. Version Compatibility

| SDK Version | .NET 6 | .NET 7 | .NET 8 | Notes |
|-------------|--------|--------|--------|-------|
| 1.4.2 | ✅ | ✅ | ✅ | Current release, multi-provider SMS |
| 1.4.0 | ✅ | ✅ | ✅ | Added Azure Communication Services |
| 1.3.0 | ✅ | ✅ | ✅ | Added AWS SNS |
| 1.2.0 | ✅ | ✅ | ❌ | Twilio only |

### Breaking Changes

**v1.4.0**: No breaking changes. Azure SMS is additive.

### Upgrading

```bash
dotnet add package PrimusSaaS.Notifications --version 1.4.2
```

---

## 13. Next Steps

After integrating Notifications Module, consider these complementary modules:

| Module | Purpose | Docs |
|--------|---------|------|
| **[Identity Validator](/docs/modules/identity-validator)** | Add JWT/OIDC authentication with multi-issuer support | ←Previous |
| **[Logging Module](/docs/modules/logging-module)** | Add structured logging with PII masking | ←Previous |
| **[Feature Flags](/docs/modules/feature-flags)** | Control feature rollouts with percentage and user targeting | →Next |

### Full Integration Example

See the [Live Demo API](/docs/modules/live-demo-api) for a complete working example with all modules integrated.
