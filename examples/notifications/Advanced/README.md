# Notifications Advanced Example

Primus Notifications with templates, SMTP, Twilio SMS, logger, queue, and dispatch policy.

## Run
```bash
dotnet restore
dotnet run
```

## Test
- Swagger UI: `http://localhost:5000/swagger`
- POST `http://localhost:5000/notify/welcome?email=test@example.com`
- POST `http://localhost:5000/notify/sms?number=+15551234567`

## Config
- Set `Notifications:Smtp` and `Notifications:Twilio` secrets (User Secrets/Key Vault).
- Templates under `NotificationTemplates/` are validated on startup and hot-reloaded in dev.
