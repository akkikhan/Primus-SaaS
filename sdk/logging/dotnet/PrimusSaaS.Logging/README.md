# PrimusSaaS.Logging

Quick reference for the .NET logging library. See `PrimusSaaS.Logging.nuspec` for package metadata.

## Usage
```csharp
logger.Info("User logged in", new Dictionary<string, object?>
{
    ["userId"] = user.Id,
    ["email"] = user.Email
});

// Anonymous object shorthand (new ergonomic overload)
logger.Info("Order created", new { orderId = order.Id, total = order.Total });

// Exceptions + anonymous objects
logger.Error(ex, "Payment failed", new { orderId = order.Id, reason = ex.Message });
```

The logger also accepts nullable dictionaries (`Dictionary<string, object?>`) and still masks PII using your configured `PiiOptions`.

## Targets
- Console (pretty or JSON)
- File (with rotation/compression)
- Application Insights
- Serilog/NLog forwarders

## Health
- `LoggingHealthReporter` surfaces target health; map it via `app.MapPrimusLoggingHealth("/_primus/logging/health")`.
