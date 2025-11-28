# Primus Notification Module

## Overview
The Primus Notification Module is a decoupled, multi-channel notification system designed to replace hardcoded email logic with a flexible, template-based approach. It allows applications to dispatch notifications without knowing the delivery details.

## Architecture

### Core Components
1.  **Dispatcher (`NotificationService`)**: The central hub that receives notifications and routes them to the appropriate channels.
2.  **Channels (`IChannel`)**: Pluggable delivery mechanisms.
    *   **Email**: Implemented using `MailKit` (SMTP).
    *   *Future*: SMS (Twilio), Push (Firebase), Real-time (SignalR).
3.  **Templates (`ITemplateService`)**: A Liquid-based template engine (`Fluid`) that separates content from code.
4.  **Notifications (`INotification`)**: Simple data objects carrying the event payload.

## Implementation Details

### 1. Library Structure (`sdk/dotnet/Primus.Notifications`)
*   **Abstractions**: Interfaces for `INotification`, `IChannel`, `ITemplateService`.
*   **Core**: The `NotificationService` logic.
*   **Channels**: `SmtpEmailChannel` implementation.
*   **Services**: `FileTemplateService` for loading `.liquid` files.

### 2. Integration (`PrimusSaaS.Portal.Api`)
*   **Registration**: Added `AddPrimusNotifications` to `Program.cs`.
*   **Templates**: Created `Templates/` directory with `EmailSubject.liquid` and `EmailBody.liquid` for each event.
*   **Events**:
    *   `ApplicationCreated`
    *   `ModuleAssigned`
    *   `VersionPublished`

## Usage for Developers

### 1. Installation
```bash
dotnet add package Primus.Notifications
```

### 2. Configuration (Startup.cs)
```csharp
services.AddPrimusNotifications(config => {
    config.UseSmtp(options => {
        options.Host = "smtp.example.com";
        // ...
    });
    config.UseFileTemplates("./Templates");
});
```

### 3. Sending a Notification
```csharp
await _notificationService.SendAsync(new WelcomeNotification(user));
```

## Value Proposition
*   **Zero Hardcoding**: HTML content is in external files, editable without recompiling.
*   **Multi-Channel Ready**: Add SMS or Slack notifications later by just adding a new Channel class.
*   **Standard Compliant**: Uses industry-standard libraries (`MailKit`, `Fluid`).
