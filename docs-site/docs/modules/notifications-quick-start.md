---
id: notifications-quick-start
title: Notifications Module - Quick Start
sidebar_position: 30
description: 5-minute setup for sending emails and SMS with Liquid templates.
---

# Notifications Quick Start

Send templated emails and SMS in under 5 minutes using Liquid templates.

:::info Complete Data Isolation
Primus Notifications runs **entirely within your application**. Emails are sent directly from your configured SMTP server, and SMS through your Twilio account. Primus never receives, stores, or processes your notification content or recipient data.
:::

import useBaseUrl from '@docusaurus/useBaseUrl';

<div className="download-grid">
  <a className="download-btn primary" href={useBaseUrl('/downloads/notifications-minimal.zip')}>
    Minimal starter (.zip)
  </a>
  <a className="download-btn secondary" href={useBaseUrl('/downloads/notifications-minimal-swagger.json')} download>
    Swagger (minimal)
  </a>
  <a className="download-btn primary" href={useBaseUrl('/downloads/notifications-advanced.zip')}>
    Advanced starter (.zip)
  </a>
  <a className="download-btn secondary" href={useBaseUrl('/downloads/notifications-advanced-swagger.json')} download>
    Swagger (advanced)
  </a>
</div>

---

## Install

```bash
dotnet add package PrimusSaaS.Notifications
```

---

## Setup (Email)

```csharp
using PrimusSaaS.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Notifications with SMTP
builder.Services.AddPrimusNotifications(n => n
    .UseSmtp(builder.Configuration.GetSection("Notifications:Smtp"))
    .UseFileTemplates("NotificationTemplates") // folder must exist
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
      "EnableSsl": true,
      "FromAddress": "noreply@example.com",
      "FromName": "My App"
    }
  }
}
```
Replace the placeholders above with real SMTP host/credentials before running; keep secrets in user secrets or environment variables.

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
    var result = await notifications.SendAsync("WelcomeEmail", new
    {
        recipient = new { email = "test@example.com", name = "Test User" },
        app = new { name = "My App" },
        data = new { activationLink = "https://example.com/activate/abc123" }
    });
    
    return Results.Ok(new
    {
        sent = result.Success,
        mode = result.ChannelUsed ?? "logged",
        channels = result.Channels
    });
});
```

```bash
curl -X POST http://localhost:5000/test-email
```

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Add SMS with Twilio | [Advanced Features ->](/docs/modules/notifications-advanced) |
| Custom providers | [Advanced Features ->](/docs/modules/notifications-advanced) |
| Template validation | [Advanced Features ->](/docs/modules/notifications-advanced) |
| Full reference | [Notifications Reference ->](/docs/modules/notifications) |
