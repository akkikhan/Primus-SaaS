---
id: logging-quick-start
title: Logging Module - Quick Start
sidebar_position: 20
description: 5-minute setup for structured logging with PII masking.
---

# Logging Quick Start

Get structured logging with automatic PII masking running in under 5 minutes.

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
