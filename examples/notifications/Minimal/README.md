# Notifications Minimal Example

Primus Notifications using templates + SMTP (if configured) with logger fallback.

## Run
```bash
dotnet restore
dotnet run
```

## Test
- Swagger UI: `http://localhost:5000/swagger`
- POST `http://localhost:5000/notify/welcome?email=test@example.com`
  - In dev, message is logged.
  - With valid SMTP config, email is sent.

## Templates
- `NotificationTemplates/WelcomeEmail/EmailSubject.liquid`
- `NotificationTemplates/WelcomeEmail/EmailBody.liquid`

## Config
- Set `Notifications:Smtp` values (use User Secrets/Key Vault for secrets).
