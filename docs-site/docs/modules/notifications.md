---
id: notifications
title: Notifications Module
sidebar_position: 3
---

Email/SMS/queue-backed notification pipeline for .NET APIs. Provides templating (Liquid), SMTP/SendGrid/Twilio/Azure Communication Services/AWS SNS/SQS bridges, in-memory or external queues, health diagnostics, and developer-friendly defaults.

## Packages

- **.NET**: `PrimusSaaS.Notifications` (DI registration, channels, templates, queue)

Version source of truth: [Modules Version Matrix](/docs/modules/version-matrix).

## Quick Install

```bash
dotnet add package PrimusSaaS.Notifications
```

## Environment / appsettings template

```json
{
  "Notifications": {
    "Templates": {
      "Path": "NotificationTemplates",
      "Watch": true
    },
    "Queue": {
      "Provider": "InMemory",
      "BoundedCapacity": 500,
      "MaxParallelHandlers": 2,
      "BaseRetryDelayMs": 250
    },
    "Smtp": {
      "Host": "<SMTP_HOST>",
      "Port": 587,
      "Username": "<SMTP_USERNAME>",
      "Password": "<SMTP_PASSWORD>",
      "EnableSsl": true,
      "FromAddress": "no-reply@example.com",
      "FromName": "Primus Notifications"
    },
    "Twilio": {
      "AccountSid": "<TWILIO_SID>",
      "AuthToken": "<TWILIO_TOKEN>",
      "FromNumber": "<TWILIO_FROM>"
    }
  }
}
```

## .NET wiring (minimal)

```csharp
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusNotifications(notifications =>
{
    var templatesPath = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");
    notifications.UseFileTemplates(templatesPath, validateOnStartup: true, watchForChanges: builder.Environment.IsDevelopment());
    notifications.UseLogger(); // always log deliveries for dev
    notifications.UseInMemoryQueue(options =>
    {
        options.BoundedCapacity = 500;
        options.MaxParallelHandlers = 2;
        options.BaseRetryDelayMs = 250;
    });
    notifications.UseSmtp(opts =>
    {
        opts.Host = builder.Configuration["Notifications:Smtp:Host"] ?? "";
        opts.Port = builder.Configuration.GetValue("Notifications:Smtp:Port", 587);
        opts.Username = builder.Configuration["Notifications:Smtp:Username"] ?? "";
        opts.Password = builder.Configuration["Notifications:Smtp:Password"] ?? "";
        opts.EnableSsl = builder.Configuration.GetValue("Notifications:Smtp:EnableSsl", true);
        opts.FromAddress = builder.Configuration["Notifications:Smtp:FromAddress"] ?? "no-reply@example.com";
        opts.FromName = builder.Configuration["Notifications:Smtp:FromName"] ?? "Primus Notifications";
    });
    notifications.UseTwilio(builder.Configuration, "Notifications:Twilio", validateOnStartup: false);
    notifications.ConfigureDispatch(opts =>
    {
        opts.ThrowOnFailure = true;
        opts.FallbackToLogger = false;
        opts.QueueOnFailure = false;
    });
});

var app = builder.Build();
app.MapPost("/notifications/welcome", async (SendWelcomeRequest request, INotificationService notifications) =>
{
    var notification = new BasicNotification(
        type: "Welcome",
        data: new { request.Name },
        recipient: new Recipient { Email = request.Email, Name = request.Name },
        channels: new[] { "Email", "Logger" });

    var result = await notifications.SendAsync(notification);
    return result.Success
        ? Results.Ok(new { message = "Notification dispatched", channel = result.ChannelUsed, queued = result.EnqueuedForRetry })
        : Results.Problem(result.FailureReason ?? "Failed to dispatch notification.");
});

app.Run();

record SendWelcomeRequest(string Email, string Name);
```

## Features
- Email: SMTP, SendGrid, SES; SMS: Twilio, AWS SNS/ACS; logger fallback for all channels.
- Templates: Liquid files per type/channel with optional hot reload.
- Queue: in-memory (default) or Redis/Service Bus for prod scale; retry with backoff.
- Health: `NotificationHealthService` for status snapshots; detailed failure reasons on send.
- Safety: bounded queues, retry limits, template validation on startup, optional webhook dispatch.

## Validation checklist
- [ ] Templates folder present and renders without errors.
- [ ] At least one channel configured (SMTP or Twilio) or logger enabled for local dev.
- [ ] Queue configured for production (Redis/ServiceBus) instead of in-memory.
- [ ] Sensitive secrets stored in Key Vault/User Secrets, not committed.
- [ ] Dispatch logs monitored in staging (failures surface explicit reasons).
