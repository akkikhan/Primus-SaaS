# Template Guide

PrimusSaaS.Notifications uses Liquid templates (via Fluid) to render subjects and bodies per notification type and channel.

## Layout and conventions
- Base path: set through `.UseFileTemplates(basePath)`
- Per-notification folder: `{basePath}/{NotificationType}`
- Expected files:
  - `EmailSubject.liquid`
  - `EmailBody.liquid`
  - `SmsBody.liquid` (optional; used by the Sms channel)
- Channel names must match the `INotification.Channels` entries (case-insensitive).
- The built-in helpers (`SendEmailAsync(to, subject, body)` / `SendSmsAsync(to, message)`) use notification types `primus.email.direct` and `primus.sms.direct` and skip template lookup entirely.

Example structure:
```
NotificationTemplates/
  Welcome/
    EmailSubject.liquid
    EmailBody.liquid
  PasswordReset/
    EmailSubject.liquid
    EmailBody.liquid
```

## Example templates
`NotificationTemplates/Welcome/EmailSubject.liquid`
```
Welcome to Primus, {{ Name }}!
```

`NotificationTemplates/Welcome/EmailBody.liquid`
```html
<h1>Hello {{ Name }}</h1>
<p>We are excited to have you onboard.</p>
<p>Visit your dashboard: <a href="{{ DashboardUrl }}">{{ DashboardUrl }}</a></p>
```

Model values come from `INotification.Data`. Anonymous objects and POCOs are supported.

## Behavior and caching
- Templates are cached after the first successful parse; file reads happen only once per process.
- If a template is missing, the channel logs a warning and falls back to the raw data string (subject defaults to `"Notification"`).
- Parsing errors throw to surface invalid templates early.
- `FluidParser` uses the model type to register member access; prefer simple DTOs/records for clarity.
- Enable startup validation with `.UseFileTemplates(basePath, validateOnStartup: true)` to parse all templates at boot and fail fast on syntax errors.

## Tips
- Keep template names aligned with channel names to simplify multi-channel notifications.
- Include absolute URLs in emails; Liquid does not rewrite links.
- For multi-tenant scenarios, set `basePath` to a tenant-specific folder when calling `.UseFileTemplates(...)`.
- Validate templates in CI by loading them through `FileTemplateService.RenderAsync` with representative models.
