# Primus Logging: Health & Correlation Samples

## Health Endpoint (aspnetcore)
```csharp
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPrimusLogging(opts => { /* ... */ });

var app = builder.Build();
app.MapPrimusLoggingHealth("/_primus/logging/health"); // protect in prod
app.Run();
```

## Correlation & Scopes
- Middleware emits `X-Request-ID` and `X-Correlation-ID` on every response and scopes logs with those values.
- Upstream services can pass `X-Correlation-ID`; middleware will reuse it.

## Metrics
- Access programmatically: `logger.GetMetricsSnapshot()`; includes Writes, Drops, Failures.
- Health endpoint returns metrics + targets as JSON for quick diagnostics.
