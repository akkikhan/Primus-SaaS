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

<div style={{ display: 'grid', gap: '0.5rem', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', maxWidth: '560px', margin: '1rem auto', alignItems: 'stretch' }}>
  <a
    className="button button--primary button--sm"
    style={{ fontWeight: 700, textAlign: 'center', background: '#a20000', color: '#ffffff', border: '1px solid #a20000', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
    href={useBaseUrl('/downloads/logging-minimal.zip')}>
    Minimal starter (.zip)
  </a>
  <a
    className="button button--secondary button--sm"
    style={{ fontWeight: 700, textAlign: 'center', background: '#ffffff', color: '#a20000', border: '1px solid #a20000', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
    href={useBaseUrl('/downloads/logging-minimal-swagger.json')} download>
    Swagger (minimal)
  </a>
  <a
    className="button button--primary button--sm"
    style={{ fontWeight: 700, textAlign: 'center', background: '#a20000', color: '#ffffff', border: '1px solid #a20000', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
    href={useBaseUrl('/downloads/logging-advanced.zip')}>
    Advanced starter (.zip)
  </a>
  <a
    className="button button--secondary button--sm"
    style={{ fontWeight: 700, textAlign: 'center', background: '#ffffff', color: '#a20000', border: '1px solid #a20000', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
    href={useBaseUrl('/downloads/logging-advanced-swagger.json')} download>
    Swagger (advanced)
  </a>
</div>

---

## Install

```bash
dotnet add package PrimusSaaS.Logging
```

---

## Setup (3 Lines)

```csharp
using PrimusSaaS.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add this ONE line
builder.Logging.AddPrimusLogging(opts => 
    builder.Configuration.GetSection("PrimusLogging").Bind(opts));

var app = builder.Build();
app.Run();
```

---

## Configure

### appsettings.json (Console Output)

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Information",
    "Targets": ["Console"],
    "EnablePiiMasking": true
  }
}
```

### appsettings.json (Application Insights)

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Information",
    "Targets": ["Console", "ApplicationInsights"],
    "ApplicationInsights": {
      "ConnectionString": "InstrumentationKey=xxx"
    },
    "EnablePiiMasking": true
  }
}
```

### appsettings.json (File Output)

```json
{
  "PrimusLogging": {
    "MinimumLevel": "Information",
    "Targets": ["Console", "File"],
    "File": {
      "Path": "logs/app-.log",
      "RollingInterval": "Day"
    },
    "EnablePiiMasking": true
  }
}
```

### Optional: cut noise, keep errors, and mask sensitive fields

```csharp
using PrimusSaaS.Logging.Extensions;

builder.Logging.AddPrimus(opts =>
{
    opts.TruncateCategoryNames = false;   // keep full category names (avoid redaction)
    opts.SamplingRate = 0.10;             // keep 10% of low-importance logs
    opts.AlwaysLogOnError = true;         // never sample out errors
    opts.MaskFields = new() { "password", "token", "apiKey", "ssn" };
});
```

---

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
[2024-01-15 10:30:45 INF] Processing order ORD-12345 for j***@example.com
```

PII like email addresses and credit card numbers are automatically masked!

---

## Next Steps

| Want to... | See Guide |
|------------|-----------|
| Custom PII patterns | [Advanced Features →](/docs/modules/logging-advanced) |
| Correlation IDs | [Advanced Features →](/docs/modules/logging-advanced) |
| Custom enrichers | [Advanced Features →](/docs/modules/logging-advanced) |
| Full reference | [Logging Module Reference →](/docs/modules/logging-module) |
