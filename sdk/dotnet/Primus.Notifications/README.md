# Primus.Notifications

Production-ready notification building blocks for Primus SaaS applications. The library provides SMTP email delivery, file-based templating with Fluid, an in-memory queue with a background worker, and instrumentation hooks for observability.

## Features
- SMTP email channel with retry/backoff and structured logging
- File-based Liquid templates with caching and subject/body conventions
- In-memory bounded queue plus background worker for async delivery
- Lightweight diagnostics via `NotificationMetrics` and `NotificationRuntimeStats`
- Opt-in logger channel for non-email scenarios or local development

## Installation
```bash
dotnet add package Primus.Notifications --version 1.0.0
```

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
await notificationService.SendAsync(notification, cancellationToken);
```

Send one-off notifications without creating a custom `INotification` type:
```csharp
await notificationService.SendEmailAsync("user@example.com", "Welcome", "<p>Thanks for signing up!</p>");
await notificationService.SendSmsAsync("+15551234567", "Your code is 123456");
```

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

> **Note:** For production, store your Auth Token in Azure Key Vault, environment variables, or user secrets—never in source code.

## SMTP configuration
`SmtpOptions` supports host, port, credentials, SSL, sender info, timeout, retry count, and exponential backoff base delay. Validation runs during DI configuration to catch missing host/port/from settings early.

## Templates
- File layout: `{BasePath}/{NotificationType}/EmailSubject.liquid` and `EmailBody.liquid`
- Fluid syntax with anonymous/POCO models
- Templates are cached after first parse for performance
- See `TEMPLATE_GUIDE.md` for conventions and examples

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
