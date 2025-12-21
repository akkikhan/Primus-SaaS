---
id: logging-quick-start
title: Logging Module - Quick Start
sidebar_position: 20
description: 5-minute setup for structured logging with PII masking.
---

# Logging Quick Start

Get structured logging with automatic PII masking running in under 5 minutes.

:::info Complete Data Isolation
Primus Logging runs **entirely within your application**. All logs are written to targets you configure (Console, File, Application Insights). Primus never receives, stores, or processes your log data.
:::

import useBaseUrl from '@docusaurus/useBaseUrl';

<div className="download-grid">
  <a className="download-btn primary" href={useBaseUrl('/downloads/logging-minimal.zip')}>
    Minimal starter (.zip)
  </a>
  <a className="download-btn secondary" href={useBaseUrl('/downloads/logging-minimal-swagger.json')} download>
    Swagger (minimal)
  </a>
  <a className="download-btn primary" href={useBaseUrl('/downloads/logging-advanced.zip')}>
    Advanced starter (.zip)
  </a>
  <a className="download-btn secondary" href={useBaseUrl('/downloads/logging-advanced-swagger.json')} download>
    Swagger (advanced)
  </a>
</div>

---

## Install

```bash
dotnet add package PrimusSaaS.Logging --version 1.2.4
```

---

## Setup (3 lines)

```csharp
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Replace default providers and bind Primus config
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(builder.Configuration.GetSection("PrimusLogging"));

var app = builder.Build();

// Optional middleware for request logging + correlation IDs
// app.UsePrimusLogging();

app.Run();
```

> Tip: use `ClearProviders()` if you want to disable the default console/debug loggers and emit only what Primus is configured to write.

---

## Configure

### appsettings.json (Console Output)

```json
{
  "PrimusLogging": {
    "ApplicationId": "MyService",
    "Environment": "Development",
    "MinLevel": 1,
    "Targets": [
      {
        "Type": "console",
        "Pretty": true
      }
    ],
    "Pii": {
      "MaskEmails": true,
      "MaskCreditCards": true,
      "MaskSSN": true,
      "MaskPasswords": true,
      "MaskTokens": true,
      "MaskSecrets": true
    }
  }
}
```

### appsettings.json (Application Insights)

```json
{
  "PrimusLogging": {
    "ApplicationId": "MyService",
    "Environment": "Production",
    "MinLevel": 1,
    "Targets": [
      { "Type": "console", "Pretty": true },
      { "Type": "applicationInsights", "ConnectionString": "InstrumentationKey=xxx" }
    ],
    "Pii": { "MaskEmails": true, "MaskCreditCards": true, "MaskSSN": true }
  }
}
```

### appsettings.json (File Output)

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
      }
    ],
    "Pii": { "MaskEmails": true, "MaskCreditCards": true, "MaskSSN": true }
  }
}
```

## Use

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }

    public void ProcessOrder(string orderId, string customerEmail)
    {
        // Structured logging with automatic PII masking
        _logger.LogInformation(
            "Processing order {OrderId} for {CustomerEmail}",
            orderId,
            customerEmail  // Auto-masked if contains @
        );
    }
}
```

---

## Output Example

```
[2024-01-15 10:30:45 INF] Processing order ORD-12345 for ***REDACTED***
```

PII like email addresses and credit card numbers are automatically masked!

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Custom PII patterns | [Advanced Features ->](/docs/modules/logging-advanced) |
| Correlation IDs | [Advanced Features ->](/docs/modules/logging-advanced) |
| Custom enrichers | [Advanced Features ->](/docs/modules/logging-advanced) |
| Full reference | [Logging Module Reference ->](/docs/modules/logging-module) |
