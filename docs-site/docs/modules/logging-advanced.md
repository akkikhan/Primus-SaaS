---
id: logging-advanced
title: Logging Module - Advanced Features
sidebar_position: 21
description: PII masking controls, correlation IDs, enrichers, and sampling for Primus Logging.
---

# Logging Advanced Features

Keep the advanced options aligned with the shipped SDK: correlation middleware, PII masking, enrichers, and sampling.

---

## Baseline wiring

```csharp
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddPrimus(opts =>
    builder.Configuration.GetSection("PrimusLogging").Bind(opts));

var app = builder.Build();

// Request logging + correlation IDs (adds X-Correlation-ID header)
app.UsePrimusLogging();

app.MapControllers();
app.Run();
```

`ApplicationId` in `PrimusLogging` tags every log with the service name.

---

## PII masking

Control masking via config and optional custom patterns/keys.

```json
{
  "PrimusLogging": {
    "Pii": {
      "MaskEmails": true,
      "MaskCreditCards": true,
      "MaskSSN": true,
      "MaskPasswords": true,
      "MaskTokens": true,
      "MaskSecrets": true,
      "CustomSensitiveKeys": ["apiKey", "secret"],
      "CustomRegexPatterns": ["ORD-\\d{8}"]
    }
  }
}
```

---

## Correlation IDs

`app.UsePrimusLogging()` issues/propagates `X-Correlation-ID` and adds it to logs. Downstream services can read the header and forward it on outgoing requests if needed.

---

## Enrichers (add context)

Use built-in enrichers to append context to every log entry.

```csharp
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

builder.Logging.AddPrimus(opts =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(opts);

    // Static properties
    opts.Enrichers.Add(new PropertyEnricher("Application", "OrderService"));
    opts.Enrichers.Add(new PropertyEnricher("Environment", builder.Environment.EnvironmentName));

    // Machine/thread info
    opts.Enrichers.Add(new MachineNameEnricher());
    opts.Enrichers.Add(new ThreadIdEnricher());
});
```

For per-request context, use `ILogger.BeginScope` in your code to push properties for that request.

---

## Sampling and category length

```csharp
builder.Logging.AddPrimus(opts =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(opts);
    opts.SamplingRate = 0.10;      // keep 10% of low-importance logs
    opts.AlwaysLogOnError = true;  // never sample out errors
    opts.TruncateCategoryNames = true;
    opts.MaxCategoryLength = 120;  // default
});
```

---

## Multiple targets (example)

```json
{
  "PrimusLogging": {
    "ApplicationId": "MyService",
    "Environment": "Production",
    "MinLevel": 1,
    "Targets": [
      { "Type": "console", "Pretty": true },
      {
        "Type": "file",
        "Path": "logs/app-.log",
        "Async": true,
        "MaxFileSize": 10485760,
        "MaxRetainedFiles": 5,
        "CompressRotatedFiles": true
      },
      {
        "Type": "applicationInsights",
        "ConnectionString": "your-connection-string"
      }
    ],
    "Pii": {
      "MaskEmails": true,
      "MaskCreditCards": true,
      "MaskSSN": true
    }
  }
}
```

---

## Structured logging tips

- Use message templates with named properties (`_logger.LogInformation("Order {OrderId}", orderId)`).
- Use scopes (`using _logger.BeginScope(new Dictionary<string,object>{{"OrderId", order.Id}}))`) to add request-specific context.
- Pass exceptions as the first parameter (`_logger.LogError(ex, "Payment failed for {OrderId}", order.Id)`).
