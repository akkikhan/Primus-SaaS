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

## Noise + safety controls
- `TruncateCategoryNames` / `MaxCategoryLength`: disable redaction or cap category length to keep logs readable.
- `SamplingRate` + `AlwaysLogOnError`: sample down noisy logs while never dropping errors/criticals (probability 0.0 - 1.0).
- `MaskFields`: explicitly mask sensitive keys (e.g., `["password","token","apiKey","ssn"]`) in addition to built-in PII masking.

## Targets
- Console (pretty or JSON)
- File (with rotation/compression)
- Application Insights
- Serilog/NLog forwarders

## Health
- `LoggingHealthReporter` surfaces target health; map it via `app.MapPrimusLoggingHealth("/_primus/logging/health")`.
