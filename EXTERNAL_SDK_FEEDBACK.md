# External SDK Feedback (Action Notes)

## Auth0 .NET
- Provide an option/package bundle that brings in `Auth0.ManagementApi` alongside `Auth0.AspNetCore.Authentication`.
- Add a short guide/sample for multi-tenant configuration (multiple domains/issuers) to reduce confusion.

## Twilio .NET
- Expose an `ITwilioClient`/`ITwilioSmsSender` abstraction to enable DI/testing.
- Provide built-in retry with backoff (polly or built-in) around SMS send calls; allow HttpClient injection.

## Azure.Communication.Sms
- Add managed identity support for SMS client initialization in addition to connection string.
- Expand supported country coverage; document limitations clearly.

## AWS SNS SDK
- Add a convenience `SendSmsAsync(phone, message, attrs?)` helper that wraps `PublishAsync` with SMS attributes.

## Fluid.Core
- Offer an optional FileSystemWatcher to hot-reload templates on change for local/dev scenarios.
