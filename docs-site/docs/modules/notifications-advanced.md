---
id: notifications-advanced
title: Notifications Module - Advanced Setup
sidebar_position: 31
description: Step-by-step email + SMS setup with the Primus Notifications package.
---

# Notifications Advanced Setup

A concise, package-only path to ship email and SMS with Primus Notifications. Follow these steps in order so the code, config, and templates line up.

---

## 1) Install the package

```bash
dotnet add package PrimusSaaS.Notifications
```

---

## 2) Register services (Program.cs)

```csharp
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;

var builder = WebApplication.CreateBuilder(args);
var templatesRoot = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");

builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseFileTemplates(templatesRoot, validateOnStartup: true);
    notifications.UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"));
    notifications.UseTwilio(opts => builder.Configuration.GetSection("Notifications:Twilio").Bind(opts));
    notifications.UseLogger(); // safe fallback in dev/test
});

var app = builder.Build();
app.MapControllers();
app.Run();
```

---

## 3) Add configuration (appsettings.json)

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "smtp-user",
      "Password": "use-user-secrets-not-here",
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

Use real credentials (store in user secrets/Key Vault). Twilio `FromNumber` must be E.164 (for example, `+15551234567`).

---

## 4) Create templates

```
NotificationTemplates/
  PasswordReset/
    EmailSubject.liquid
    EmailBody.liquid
    SmsBody.liquid
```

**NotificationTemplates/PasswordReset/EmailSubject.liquid**
```
Reset your {{ app.name }} password
```

**NotificationTemplates/PasswordReset/EmailBody.liquid**
```liquid
<p>Hi {{ recipient.name }},</p>
<p>Use this code to reset your password: <strong>{{ data.code }}</strong></p>
<p>This code expires in {{ data.expiryMinutes }} minutes.</p>
```

**NotificationTemplates/PasswordReset/SmsBody.liquid**
```liquid
{{ app.name }} code: {{ data.code }} (expires in {{ data.expiryMinutes }} minutes)
```

---

## 5) Send an email + SMS

```csharp
using PrimusSaaS.Notifications.Abstractions;

public class AccountNotifications
{
    private readonly INotificationService _notifications;

    public AccountNotifications(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task SendPasswordReset(string email, string phone, string code)
    {
        var context = new
        {
            recipient = new { email, phone, name = "User" },
            app = new { name = "My App" },
            data = new { code, expiryMinutes = 10 }
        };

        var result = await _notifications.SendAsync("PasswordReset", context);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.FailureReason ?? "Notification failed");
        }
    }
}
```

The `PasswordReset` templates drive both channels. If SMS or email templates are missing, that channel is skipped; when `UseLogger` is enabled you still get a logged payload for validation.

---

## 6) Validate the setup quickly

- Run `dotnet run` and hit your endpoint once; check logs to confirm `Notifications` logged/sent (SMTP/Twilio) and that no template errors appeared.
- Add a light dev-only probe so you can see whether templates and providers load:
  ```csharp
  app.MapGet("/primus/notifications/health", () => Results.Ok("notifications ready"));
  ```
- Keep `UseLogger` enabled while you wire things up; switch it off only after a real send succeeds.

---

## Next Steps
- Need the shortest path? See `modules/notifications-quick-start`.
- Want every option? See `modules/notifications`.
