# PrimusSaaS.Notifications

Production-ready notification building blocks for Primus SaaS applications. The library provides SMTP email delivery, file-based templating with Fluid, an in-memory queue with a background worker, and instrumentation hooks for observability.

## Features
- SMTP email channel with retry/backoff and structured logging
- File-based Liquid templates with caching and subject/body conventions
- In-memory bounded queue plus background worker for async delivery
- Lightweight diagnostics via `NotificationMetrics` and `NotificationRuntimeStats`
- Opt-in logger channel for non-email scenarios or local development
- `NotificationResult` surface that reports per-channel outcomes and can throw on failure to prevent silent drops

## Installation
```bash
dotnet add package PrimusSaaS.Notifications --version 1.3.0
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
        .UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates"))
        .UseInMemoryQueue(options =>
        {
            options.BoundedCapacity = 500;
            options.MaxParallelHandlers = 2;
        })
        .UseSms() // logs SMS payloads by default; swap ISmsSender for your provider
        .UseLogger();
});
```

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

Environment variables (examples):
- `Twilio__AccountSid=ACxxxx`
- `Twilio__AuthToken=...`
- `Twilio__FromNumber=+16205538468`

> Notes: Twilio trial accounts require verifying each recipient number before sending. Keep AuthToken in secrets/Key Vault/env vars—never commit secrets.
> Twilio options are validated during startup (can be disabled with `ValidateOnStartup = false`).

## SMTP configuration
`SmtpOptions` supports host, port, credentials, SSL, sender info, timeout, retry count, and exponential backoff base delay. Validation runs during DI configuration to catch missing host/port/from settings early.

## Templates
- File layout: `{BasePath}/{NotificationType}/EmailSubject.liquid` and `EmailBody.liquid`
- SMS layout: `{BasePath}/{NotificationType}/SmsBody.liquid` (optional)
- Fluid syntax with anonymous/POCO models
- Templates are cached after first parse for performance
- See `TEMPLATE_GUIDE.md` for conventions and examples

Direct helpers (`SendEmailAsync(to, subject, body)` / `SendSmsAsync(phone, message)`) skip template lookup entirely to avoid confusing failures when you just want to send raw content.

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
