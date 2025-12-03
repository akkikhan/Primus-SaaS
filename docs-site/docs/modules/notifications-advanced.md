---
id: notifications-advanced
title: Notifications Module - Advanced Features
sidebar_position: 31
description: SMS integration, custom providers, template partials, localization, and delivery tracking.
---

# Notifications Advanced Features

Add SMS support, custom providers, template partials, localization, and delivery tracking.

---

## SMS with Twilio

### Configure Twilio Provider

```csharp
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"))
    .UseTwilio(opts => builder.Configuration.GetSection("Notifications:Twilio").Bind(opts))
    .UseFileTemplates("NotificationTemplates")
    .UseLogger());
```

### appsettings.json

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "smtp-user",
      "Password": "smtp-password",
      "UseSsl": true,
      "FromEmail": "noreply@example.com"
    },
    "Twilio": {
      "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
      "AuthToken": "your-auth-token",
      "FromNumber": "+15551234567"
    }
  }
}
```
Use real credentials (not placeholders) and keep secrets in user secrets or environment variables. `FromNumber` must be E.164 (for example, `+15551234567`)-spaces/dashes will fail.

### Create SMS Template

#### NotificationTemplates/PasswordReset/SmsBody.liquid

```liquid
Your {{ app.name }} verification code is: {{ data.code }}. Expires in {{ data.expiryMinutes }} minutes.
```

### Send Email + SMS

```csharp
public async Task SendPasswordReset(string email, string phone, string code)
{
    var context = new
    {
        recipient = new { email, phone, name = "User" },
        app = new { name = "My App" },
        data = new { code, expiryMinutes = 10 }
    };
    
    // Sends both email and SMS if templates exist
    await _notifications.SendAsync("PasswordReset", context);
}
```

## Operational checks and responses

- Health: `GET /primus/notifications/health` to verify template path, SMTP, and Twilio are configured before sending.
- Responses: When fallback-to-logger is enabled, check `mode`/`channels` in the response to see whether a notification was delivered or only logged.

---

## Template Partials

Create reusable template components.

### Folder Structure

```
NotificationTemplates/
  Partials/
    email_header.liquid
    email_footer.liquid
    button.liquid
  WelcomeEmail/
    EmailSubject.liquid
    EmailBody.liquid
  PasswordReset/
    EmailSubject.liquid
    EmailBody.liquid
    SmsBody.liquid
```

### Partials/email_header.liquid

```liquid
<!DOCTYPE html>
<html>
<head>
  <style>
    body { font-family: Arial, sans-serif; }
    .container { max-width: 600px; margin: 0 auto; }
    .header { background: #4F46E5; color: white; padding: 20px; }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <h1>{{ app.name }}</h1>
    </div>
    <div class="content">
```

### Partials/email_footer.liquid

```liquid
    </div>
    <div class="footer" style="margin-top: 20px; color: #666; font-size: 12px;">
      <p>© {{ 'now' | date: '%Y' }} {{ app.name }}. All rights reserved.</p>
      <p>
        <a href="{{ app.url }}/unsubscribe">Unsubscribe</a> | 
        <a href="{{ app.url }}/privacy">Privacy Policy</a>
      </p>
    </div>
  </div>
</body>
</html>
```

### Partials/button.liquid

```liquid
<a href="{{ url }}" style="
  display: inline-block;
  background: #4F46E5;
  color: white;
  padding: 12px 24px;
  text-decoration: none;
  border-radius: 4px;
">{{ text }}</a>
```

### Using Partials

#### WelcomeEmail/EmailBody.liquid

```liquid
{% include 'Partials/email_header' %}

<h2>Welcome, {{ recipient.name }}!</h2>

<p>Thanks for joining {{ app.name }}. We're excited to have you!</p>

<p>Click below to activate your account:</p>

{% include 'Partials/button' url: data.activationLink, text: 'Activate Account' %}

<p>If you didn't create this account, please ignore this email.</p>

{% include 'Partials/email_footer' %}
```

---

## Localization

### Folder Structure for Localized Templates

```
NotificationTemplates/
  WelcomeEmail/
    EmailSubject.liquid          # Default (English)
    EmailBody.liquid
    EmailSubject.es.liquid       # Spanish
    EmailBody.es.liquid
    EmailSubject.fr.liquid       # French
    EmailBody.fr.liquid
```

### Send Localized Notification

```csharp
public async Task SendWelcomeEmail(string email, string name, string locale)
{
    var context = new
    {
        recipient = new { email, name },
        app = new { name = "My App" },
        data = new { activationLink = "..." }
    };
    
    await _notifications.SendAsync("WelcomeEmail", context, new NotificationOptions
    {
        Locale = locale  // "es", "fr", etc.
    });
}
```

### Configure Default Locale

```csharp
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"))
    .UseFileTemplates("NotificationTemplates", opts =>
    {
        opts.DefaultLocale = "en";
        opts.FallbackToDefault = true;  // Use default if locale not found
    })
    .UseLogger());
```

---

## Delivery Tracking

### Enable Delivery Tracking

```csharp
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"))
    .UseFileTemplates("NotificationTemplates")
    .UseDeliveryTracking(opts =>
    {
        opts.StoreInDatabase = true;
        opts.RetentionDays = 30;
    })
    .UseLogger());
```

### Query Delivery Status

```csharp
public class NotificationController : ControllerBase
{
    private readonly INotificationTracker _tracker;

    public NotificationController(INotificationTracker tracker)
    {
        _tracker = tracker;
    }

    [HttpGet("status/{notificationId}")]
    public async Task<IActionResult> GetStatus(string notificationId)
    {
        var status = await _tracker.GetStatusAsync(notificationId);
        return Ok(status);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? recipient,
        [FromQuery] string? type,
        [FromQuery] int days = 7)
    {
        var history = await _tracker.GetHistoryAsync(new DeliveryQuery
        {
            Recipient = recipient,
            NotificationType = type,
            FromDate = DateTime.UtcNow.AddDays(-days)
        });
        
        return Ok(history);
    }
}
```

### Delivery Webhooks

```csharp
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"))
    .UseFileTemplates("NotificationTemplates")
    .OnDeliverySuccess(async (context, result) =>
    {
        await LogDeliveryAsync(context.NotificationId, "Success", result.Provider);
    })
    .OnDeliveryFailed(async (context, error) =>
    {
        await LogDeliveryAsync(context.NotificationId, "Failed", error.Message);
        await AlertOpsTeamAsync(context, error);
    })
    .UseLogger());
```

---

## Scheduling Notifications

### Configure Scheduling

```csharp
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"))
    .UseFileTemplates("NotificationTemplates")
    .UseScheduler(opts =>
    {
        opts.UseHangfire();  // Or UseQuartz()
    })
    .UseLogger());
```

### Schedule a Notification

```csharp
public async Task ScheduleReminder(string email, string name, DateTime sendAt)
{
    var context = new
    {
        recipient = new { email, name },
        app = new { name = "My App" },
        data = new { reminderMessage = "Don't forget your appointment!" }
    };
    
    await _notifications.ScheduleAsync("Reminder", context, sendAt);
}
```

### Recurring Notifications

```csharp
// Send weekly digest every Monday at 9 AM
await _notifications.ScheduleRecurringAsync(
    "WeeklyDigest",
    context,
    "0 9 * * MON"  // Cron expression
);
```

---

## Batch Sending

### Send to Multiple Recipients

```csharp
public async Task SendAnnouncement(List<string> emails, string message)
{
    var recipients = emails.Select(e => new { email = e }).ToList();
    
    await _notifications.SendBatchAsync("Announcement", recipients, new
    {
        app = new { name = "My App" },
        data = new { message }
    }, new BatchOptions
    {
        BatchSize = 100,
        DelayBetweenBatches = TimeSpan.FromSeconds(1)
    });
}
```

---

## Preview Templates

### Preview Endpoint

```csharp
app.MapGet("/admin/notifications/preview/{type}", async (
    string type,
    INotificationRenderer renderer) =>
{
    var sampleContext = new
    {
        recipient = new { email = "sample@example.com", name = "Sample User" },
        app = new { name = "My App", url = "https://example.com" },
        data = new { code = "123456", link = "https://example.com/reset" }
    };
    
    var preview = await renderer.RenderAsync(type, sampleContext);
    
    return Results.Content(preview.EmailBody, "text/html");
});
```

### CLI Preview Tool

```csharp
// Program.cs - Add preview command for development
if (args.Contains("--preview-notification"))
{
    var type = args.SkipWhile(a => a != "--preview-notification").Skip(1).First();
    var renderer = app.Services.GetRequiredService<INotificationRenderer>();
    
    var preview = await renderer.RenderAsync(type, GetSampleContext(type));
    Console.WriteLine(preview.EmailBody);
    return;
}
```

---

## Complete Example

```csharp
using Primus.Notifications;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPrimusNotifications(n => n
    // Email provider
    .UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"))
    // SMS provider
    .UseTwilio(opts => builder.Configuration.GetSection("Notifications:Twilio").Bind(opts))
    // Templates
    .UseFileTemplates("NotificationTemplates", opts =>
    {
        opts.DefaultLocale = "en";
        opts.FallbackToDefault = true;
    })
    // Delivery tracking
    .UseDeliveryTracking(opts => opts.RetentionDays = 30)
    // Rate limiting
    .UseRateLimiting(opts =>
    {
        opts.MaxPerRecipient = 10;
        opts.WindowMinutes = 60;
    })
    // Events
    .OnDeliverySuccess(async (ctx, result) =>
    {
        Console.WriteLine($"Sent {ctx.NotificationType} via {result.Provider}");
    })
    .OnDeliveryFailed(async (ctx, error) =>
    {
        Console.WriteLine($"Failed to send {ctx.NotificationType}: {error.Message}");
    })
    .UseLogger());

var app = builder.Build();

// Send notification endpoint
app.MapPost("/notify", async (NotifyRequest request, INotificationService notifications) =>
{
    await notifications.SendAsync(request.Type, new
    {
        recipient = new { email = request.Email, phone = request.Phone, name = request.Name },
        app = new { name = "My App" },
        data = request.Data
    });
    
    return Results.Ok();
});

app.Run();

record NotifyRequest(string Type, string Email, string? Phone, string Name, Dictionary<string, object> Data);
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Basic setup | [Quick Start →](/docs/modules/notifications-quick-start) |
| Full reference | [Notifications Reference →](/docs/modules/notifications) |
