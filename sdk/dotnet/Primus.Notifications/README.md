# PrimusSaaS.Notifications

Production-ready notification building blocks for Primus SaaS applications. The library provides SMTP email delivery, SMS via Twilio/AWS SNS/Azure Communication Services, file-based templating with Fluid, an in-memory queue with a background worker, and instrumentation hooks for observability.

## Features
- SMTP email channel with retry/backoff and structured logging
- **Multi-provider SMS support**: Twilio, AWS SNS, Azure Communication Services
- File-based Liquid templates with caching and subject/body conventions
- In-memory bounded queue plus background worker for async delivery
- Lightweight diagnostics via `NotificationMetrics` and `NotificationRuntimeStats`
- Opt-in logger channel for non-email scenarios or local development
- `NotificationResult` surface that reports per-channel outcomes and can throw on failure to prevent silent drops
- Channel health service (`NotificationHealthService`) to report configuration status (email/SMS/logger)
- Optional startup template validation to catch Liquid syntax issues before runtime

## Installation
```bash
dotnet add package PrimusSaaS.Notifications --version 1.4.2
```

## 🚀 Minimal Complete Example (Copy-Paste Ready)

This is a complete, working `Program.cs` for a minimal ASP.NET Core app:

```csharp
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Configure notifications (logger mode for development)
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications
        .UseSmtp(opts =>
        {
            opts.Host = "smtp.example.com";  // Replace with your SMTP host
            opts.Port = 587;
            opts.Username = "your-username"; // Or use builder.Configuration["Smtp:Username"]
            opts.Password = "your-password"; // Or use builder.Configuration["Smtp:Password"]
            opts.FromAddress = "no-reply@yourdomain.com";
            opts.FromName = "My App";
            opts.EnableSsl = true;
        })
        .UseLogger(); // Also logs notifications (useful for development)
});

var app = builder.Build();

// Send an email
app.MapPost("/send-email", async (INotificationService notifications) =>
{
    var result = await notifications.SendEmailAsync(
        "recipient@example.com",
        "Hello from PrimusSaaS!",
        "<h1>Welcome!</h1><p>Your account has been created.</p>"
    );
    
    return result.Success 
        ? Results.Ok("Email sent!") 
        : Results.Problem($"Failed: {result.FailureReason}");
});

// Send an SMS (logs by default, configure Twilio for real SMS)
app.MapPost("/send-sms", async (INotificationService notifications) =>
{
    var result = await notifications.SendSmsAsync(
        "+15551234567",
        "Your verification code is 123456"
    );
    
    return result.Success 
        ? Results.Ok("SMS sent!") 
        : Results.Problem($"Failed: {result.FailureReason}");
});

app.Run();
```

### Using Dependency Injection (Recommended)

```csharp
// In your controller or service, inject INotificationService:
public class MyController : ControllerBase
{
    private readonly INotificationService _notifications;
    
    public MyController(INotificationService notifications)
    {
        _notifications = notifications;
    }
    
    [HttpPost("welcome")]
    public async Task<IActionResult> SendWelcome(string email, string name)
    {
        var result = await _notifications.SendEmailAsync(
            email,
            $"Welcome, {name}!",
            $"<p>Hello {name}, thanks for joining!</p>"
        );
        
        return result.Success ? Ok() : StatusCode(500, result.FailureReason);
    }
}
```

---

## Quick start (ASP.NET Core)
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications
        .UseSmtp(opts =>
        {
            opts.Host = builder.Configuration["Smtp:Host"];
            opts.Port = 587;
            opts.Username = builder.Configuration["Smtp:Username"];
            opts.Password = builder.Configuration["Smtp:Password"];
            opts.FromAddress = "no-reply@primus.local";
            opts.FromName = "Primus Notifications";
            opts.EnableSsl = true;
        })
        .UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates"), validateOnStartup: true)
        .UseInMemoryQueue(options =>
        {
            options.BoundedCapacity = 500;
            options.MaxParallelHandlers = 2;
        })
        .UseSms() // logs SMS payloads by default; swap ISmsSender for your provider
        .UseLogger()
        .ConfigureDispatch(opts =>
        {
            opts.ThrowOnFailure = true;
            opts.FallbackToLogger = false;
            opts.QueueOnFailure = true;
        });
});
```

### SMTP tuning (actual property names)
- `TimeoutSeconds` (default `30`)
- `MaxRetryCount` (default `2`)
- `RetryBaseDelayMs` (default `200`, exponential backoff)

> Older names you may see online (`Timeout`, `RetryCount`, `RetryDelayMs`) do not exist. Use the properties above.

Create a notification type:
```csharp
public record WelcomeNotification(string Email, string Name) : INotification
{
    public string Type => "Welcome";
    public object Data => new { Name };
    public IEnumerable<string> Channels => new[] { "Email", "Logger" };
    public Recipient Recipient => new() { Email = Email, Name = Name };
}
```

Render templates from `NotificationTemplates/Welcome/EmailSubject.liquid` and `NotificationTemplates/Welcome/EmailBody.liquid`, then enqueue or send directly:
```csharp
var notification = new WelcomeNotification("ada@example.com", "Ada");

// Async queue (recommended)
await queue.EnqueueAsync(notification, cancellationToken);

// Or immediate dispatch
var result = await notificationService.SendAsync(notification, cancellationToken);
if (!result.Success)
{
    // Log/return a 500 so the failure is visible
    logger.LogError("Notification failed: {Reason}", result.FailureReason);
}
```

Send one-off notifications without creating a custom `INotification` type:
```csharp
await notificationService.SendEmailAsync("user@example.com", "Welcome", "<p>Thanks for signing up!</p>");
await notificationService.SendSmsAsync("+15551234567", "Your code is 123456");
```
Both helpers return `NotificationResult` and will throw `NotificationFailedException` when `NotificationOptions.ThrowOnFailure` is enabled (default) and nothing was delivered.

Plug in your SMS provider by implementing `ISmsSender` and registering it:
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseSms<MySmsSender>(); // replaces the default logging sender
});
```

## Twilio SMS (Built-in)

The library includes a production-ready Twilio SMS sender. Configure it with your Twilio credentials:

### Option 1: Inline configuration
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseTwilio(opts =>
    {
        opts.AccountSid = builder.Configuration["Twilio:AccountSid"];
        opts.AuthToken = builder.Configuration["Twilio:AuthToken"];
        opts.FromNumber = builder.Configuration["Twilio:FromNumber"];
    });
});
```

### Option 2: From appsettings.json
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseTwilio(builder.Configuration);
});
```

```json
{
  "Twilio": {
    "AccountSid": "your-account-sid",
    "AuthToken": "your-auth-token",
    "FromNumber": "+1234567890"
  }
}
```

### Send SMS
```csharp
await notificationService.SendSmsAsync("+15551234567", "Your verification code is 123456");
```
> If Twilio credentials are missing, `NotificationResult.ServiceUnavailable` will be true. Map to HTTP 503 in your API if desired.

Environment variables (examples):
- `Twilio__AccountSid=ACxxxx`
- `Twilio__AuthToken=...`
- `Twilio__FromNumber=+16205538468`

> Notes: Twilio trial accounts require verifying each recipient number before sending. Keep AuthToken in secrets/Key Vault/env vars-never commit secrets.
> Twilio options are validated when configured (can be disabled with `ValidateOnStartup = false`). Email-only deployments without Twilio settings skip startup validation; the first SMS send will throw a helpful error if Twilio is still unconfigured. Missing credentials surface as service-unavailable.

## Inspecting results
- `NotificationResult.Channels` is the per-channel breakdown (`Channel`, `Status`, `Detail`/`Exception`). (`ChannelResults` does not exist.)
- `ChannelUsed` is set only when at least one channel succeeds.
- For HTTP APIs, map `ServiceUnavailable` to 503 if your SMS provider is offline.

## AWS SNS SMS (Built-in)

AWS Simple Notification Service (SNS) is an alternative to Twilio with global coverage and pay-per-use pricing.

### Option 1: Inline configuration
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseAwsSns(opts =>
    {
        opts.AccessKeyId = builder.Configuration["AwsSns:AccessKeyId"];
        opts.SecretAccessKey = builder.Configuration["AwsSns:SecretAccessKey"];
        opts.Region = "us-east-1";
        opts.SmsType = "Transactional"; // or "Promotional"
    });
});
```

### Option 2: From appsettings.json
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseAwsSns(builder.Configuration);
});
```

```json
{
  "AwsSns": {
    "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "Region": "us-east-1",
    "SmsType": "Transactional",
    "SenderId": "MyApp"
  }
}
```

> Notes: 
> - `SmsType` can be `Transactional` (higher priority, for OTP/alerts) or `Promotional` (marketing messages)
> - `SenderId` is optional and only supported in some countries (not US)
> - Ensure your IAM user has `sns:Publish` permission
> - Request SMS spending limits increase in AWS Support Center if needed

## Azure Communication Services SMS (Built-in)

Azure Communication Services provides SMS capabilities integrated with the Azure ecosystem.

### Option 1: Inline configuration
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseAzureCommunicationServices(opts =>
    {
        opts.ConnectionString = builder.Configuration["AzureCommunicationServices:ConnectionString"];
        opts.FromNumber = "+18001234567"; // Your provisioned Azure phone number
        opts.EnableDeliveryReport = true;
    });
});
```

### Option 2: From appsettings.json
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseAzureCommunicationServices(builder.Configuration);
});
```

```json
{
  "AzureCommunicationServices": {
    "ConnectionString": "endpoint=https://your-resource.communication.azure.com/;accesskey=...",
    "FromNumber": "+18001234567",
    "EnableDeliveryReport": true,
    "Tag": "my-app"
  }
}
```

> Notes:
> - Get a phone number in Azure Portal → Communication Services → Phone numbers
> - Connection string is in Azure Portal → Communication Services → Keys
> - `Tag` is optional and helps with analytics in Azure Portal
> - Azure SMS supports toll-free and local numbers in 180+ countries

## Choosing an SMS Provider

| Provider | Best For | Key Advantages |
|----------|----------|----------------|
| **Twilio** | Most use cases | Widest carrier support, rich features, excellent docs |
| **AWS SNS** | AWS-heavy stack | Pay-per-use, no monthly fees, great for transactional |
| **Azure** | Azure-heavy stack | Native Azure integration, combined billing |

## SMTP configuration
`SmtpOptions` supports host, port, credentials, SSL, sender info, timeout, retry count, and exponential backoff base delay. Validation runs during DI configuration to catch missing host/port/from settings early.

## Templates
- File layout: `{BasePath}/{NotificationType}/EmailSubject.liquid` and `EmailBody.liquid`
- SMS layout: `{BasePath}/{NotificationType}/SmsBody.liquid` (optional)
- Fluid syntax with anonymous/POCO models
- Templates are cached after first parse for performance
- See `TEMPLATE_GUIDE.md` for conventions and examples

Direct helpers (`SendEmailAsync(to, subject, body)` / `SendSmsAsync(phone, message)`) skip template lookup entirely to avoid confusing failures when you just want to send raw content.
Set `validateOnStartup: true` in `UseFileTemplates` to fail fast on template syntax errors during app boot.

## Health endpoint
Add a minimal endpoint to surface channel configuration status:
```csharp
app.MapGet("/health/notifications", async (NotificationHealthService health) =>
{
    var snapshot = await health.GetChannelHealthAsync();
    return Results.Json(snapshot);
});
```

## Background processing
Calling `.UseInMemoryQueue()` registers `InMemoryNotificationQueue` and `NotificationBackgroundService` to drain the queue using scoped `NotificationService` instances. Configure capacity, max parallel handlers, and retry/backoff per `NotificationQueueOptions`.

## Diagnostics
Expose metrics via `NotificationMetrics` (System.Diagnostics.Metrics instruments) and quick in-process stats via `NotificationRuntimeStats.GetSnapshot()`.

## Building and packing
```bash
dotnet test sdk/dotnet/Primus.Notifications.Tests/Primus.Notifications.Tests.csproj
dotnet pack sdk/dotnet/Primus.Notifications/Primus.Notifications.csproj -c Release
```
Packages are emitted to `nupkg/` with symbols and README included.

## Troubleshooting

### Common Issues

**"The type or namespace name 'Notifications' does not exist in the namespace 'Primus'"**

Make sure you're using the correct namespace:
```csharp
// ✅ Correct
using PrimusSaaS.Notifications;

// ❌ Wrong (old namespace)
using Primus.Notifications;
```

**"Cannot resolve INotificationService from DI container"**

Ensure you've registered the notifications services in `Program.cs`:
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseSmtp(opts => { /* ... */ });
});
```

**Email not being sent**

1. Check your SMTP credentials are correct
2. Verify port 587 (TLS) or 465 (SSL) is open
3. Check `result.Success` and `result.FailureReason`:
```csharp
var result = await notifications.SendEmailAsync(...);
if (!result.Success)
{
    Console.WriteLine($"Failed: {result.FailureReason}");
}
```
