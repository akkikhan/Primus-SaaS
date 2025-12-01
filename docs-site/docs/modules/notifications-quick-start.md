---
id: notifications-quick-start
title: Notifications Module - Quick Start
sidebar_position: 30
description: 5-minute setup for sending emails and SMS with Liquid templates.
---

# Notifications Quick Start

Send templated emails and SMS in under 5 minutes using Liquid templates.

---

## Install

```bash
dotnet add package PrimusSaaS.Notifications
```

---

## Setup (Email)

```csharp
using Primus.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Notifications with SMTP
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(opts => builder.Configuration.GetSection("Notifications:Smtp").Bind(opts))
    .UseFileTemplates("NotificationTemplates")
    .UseLogger());

var app = builder.Build();
```

---

## Configure

### appsettings.json

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "your-username",
      "Password": "your-password",
      "UseSsl": true,
      "FromEmail": "noreply@example.com",
      "FromName": "My App"
    }
  }
}
```

---

## Create a Template

### NotificationTemplates/WelcomeEmail/EmailSubject.liquid

```liquid
Welcome to {{ app.name }}, {{ recipient.name }}!
```

### NotificationTemplates/WelcomeEmail/EmailBody.liquid

```liquid
<!DOCTYPE html>
<html>
<body>
  <h1>Welcome, {{ recipient.name }}!</h1>
  <p>Thanks for joining {{ app.name }}.</p>
  <p><a href="{{ data.activationLink }}">Activate your account</a></p>
</body>
</html>
```

---

## Send a Notification

```csharp
public class AccountService
{
    private readonly INotificationService _notifications;

    public AccountService(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task SendWelcomeEmail(string email, string name, string activationLink)
    {
        await _notifications.SendAsync("WelcomeEmail", new
        {
            recipient = new { email, name },
            app = new { name = "My App" },
            data = new { activationLink }
        });
    }
}
```

---

## Test It

```csharp
app.MapPost("/test-email", async (INotificationService notifications) =>
{
    await notifications.SendAsync("WelcomeEmail", new
    {
        recipient = new { email = "test@example.com", name = "Test User" },
        app = new { name = "My App" },
        data = new { activationLink = "https://example.com/activate/abc123" }
    });
    
    return new { sent = true };
});
```

```bash
curl -X POST http://localhost:5000/test-email
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Add SMS with Twilio | [Advanced Features →](/docs/modules/notifications-advanced) |
| Custom providers | [Advanced Features →](/docs/modules/notifications-advanced) |
| Template partials | [Advanced Features →](/docs/modules/notifications-advanced) |
| Full reference | [Notifications Reference →](/docs/modules/notifications) |
