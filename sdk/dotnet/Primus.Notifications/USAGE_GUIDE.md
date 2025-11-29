# Usage Guide

Step-by-step guidance for integrating PrimusSaaS.Notifications into a service.

## 1. Configure services
```csharp
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications
        .UseSmtp(opts =>
        {
            opts.Host = builder.Configuration["Smtp:Host"];
            opts.Port = builder.Configuration.GetValue("Smtp:Port", 587);
            opts.Username = builder.Configuration["Smtp:Username"];
            opts.Password = builder.Configuration["Smtp:Password"];
            opts.FromAddress = builder.Configuration["Smtp:From"];
            opts.FromName = "Primus Notifications";
        })
        .UseSms() // default: logs SMS payloads (swap ISmsSender for real provider)
        .UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates"))
        .UseInMemoryQueue(options =>
        {
            options.BoundedCapacity = 1000;
            options.MaxParallelHandlers = 2;
            options.MaxRetryCount = 2;
            options.BaseRetryDelayMs = 200;
        })
        .UseLogger() // optional, useful for local/dev
        .ConfigureDispatch(opts => opts.ThrowOnFailure = true); // surfaces failures instead of silently succeeding
});
```

## 2. Add configuration
```json
// appsettings.json
{
  "Smtp": {
    "Host": "smtp.mailhost.local",
    "Port": 587,
    "Username": "smtp-user",
    "Password": "smtp-password",
    "From": "no-reply@primus.local"
  }
}
```

## 3. Create notification models
Implement `INotification` to describe the payload, recipient, and channels. Use the type name to resolve templates.
```csharp
public record PasswordResetNotification(string Email, string ResetLink) : INotification
{
    public string Type => "PasswordReset";
    public object Data => new { ResetLink };
    public IEnumerable<string> Channels => new[] { "Email" };
    public Recipient Recipient => new() { Email = Email };
}
```

## 4. Provide templates
Place Liquid templates under your configured base path:
```
NotificationTemplates/
  PasswordReset/
    EmailSubject.liquid
    EmailBody.liquid
```
See `TEMPLATE_GUIDE.md` for examples and variables.

## 5. Dispatch notifications
- Async (recommended): inject `INotificationQueue` and enqueue:
  ```csharp
  await notificationQueue.EnqueueAsync(notification, cancellationToken);
  ```
- Synchronous: inject `NotificationService` and call `SendAsync` if you need immediate delivery.
  ```csharp
  var result = await notificationService.SendAsync(notification, cancellationToken);
  if (!result.Success)
  {
      return Results.StatusCode(500, result.FailureReason);
  }
  ```

### One-line helpers (no templates required)
```csharp
await notificationService.SendEmailAsync("user@example.com", "Welcome", "<p>Hi there!</p>");
await notificationService.SendSmsAsync("+15551234567", "Your code is 123456");
```
These helpers build the notification contract and pick the right channel list (`Email`/`Sms`) for you.
Direct helpers bypass template resolution; they won't look for `primus.email.direct/EmailSubject.liquid`.

### Swap in your SMS provider
- Implement `ISmsSender` (e.g., Twilio, AWS SNS, MessageBird).
- Register it with the builder so it replaces the logging fallback:
  ```csharp
  builder.Services.AddPrimusNotifications(n =>
  {
      n.UseSms<YourSmsSender>();
  });
  ```

### Twilio quick start
```csharp
builder.Services.AddPrimusNotifications(n =>
{
    n.UseTwilio(opts =>
    {
        opts.AccountSid = builder.Configuration["Twilio:AccountSid"];
        opts.AuthToken = builder.Configuration["Twilio:AuthToken"];
        opts.FromNumber = builder.Configuration["Twilio:FromNumber"]; // or opts.MessagingServiceSid
        opts.ValidateOnStartup = true; // default
    });
});
```
Environment variables (examples):
- `Twilio__AccountSid=ACxxxx`
- `Twilio__AuthToken=...`
- `Twilio__FromNumber=+16205538468`

> Twilio trial accounts require verifying each recipient number before sending. Keep AuthToken in secrets/Key Vault/env vars—never commit secrets.

## 6. Operational notes
- Background processing: `.UseInMemoryQueue()` registers `NotificationBackgroundService` automatically. Ensure the hosting environment runs background services (default for ASP.NET Core).
- Retries: SMTP channel retries within a send attempt; background worker retries failed notifications using `NotificationQueueOptions`.
- Logging: the logger channel mirrors notifications to your logs for auditing or dev environments.
- Metrics: wire `NotificationMetrics` through OpenTelemetry or `MeterListener` to export counters and histograms.
- Fail-fast: `NotificationOptions.ThrowOnFailure` (enabled by default) throws `NotificationFailedException` when no channel delivers the notification so APIs don't return 200 on dropped messages.

## 7. Production checklist
- Provide SMTP credentials via secret storage (Key Vault, AWS Secrets Manager, etc.).
- Set `BoundedCapacity` high enough for burst traffic and `MaxParallelHandlers` based on SMTP throughput.
- Add dashboards for `primus_notifications_sent_total`, `primus_notifications_failed_total`, and `primus_notification_dispatch_duration_ms`.
- Validate templates in CI to catch syntax errors (see FileTemplateService behavior).
