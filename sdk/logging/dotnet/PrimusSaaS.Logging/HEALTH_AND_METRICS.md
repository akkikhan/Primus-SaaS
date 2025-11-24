# Logging Health & Metrics

## Metrics
- `LoggingMetrics` provides:
  - `WrittenEntries`
  - `DroppedEntries`
  - `WriteFailures`
- Async wrapper (`AsyncTargetWrapper`) updates metrics; use small buffer sizes to test backpressure.

## Health Signals
- If you wrap targets asynchronously, monitor:
  - `DroppedEntries` > 0 indicates backpressure or slow sinks.
  - `WriteFailures` > 0 indicates sink errors (file/Azure/etc.).
- Consider exposing metrics via your preferred monitoring (Prometheus/OpenTelemetry).

## Suggested Observability
- Log when sinks fail: already emitted to stderr.
- Use scopes for correlation (`Logger.BeginScope` or ILogger scopes).
- Include request/correlation IDs via LoggingMiddleware.
- Optionally expose metrics via your HTTP app:
  ```csharp
  app.MapGet("/primus/logging/metrics", (Logger logger) =>
  {
      return Results.Json(logger.GetMetricsSnapshot());
  });
  ```
- Optional health endpoint based on metrics:
  ```csharp
  app.MapGet("/primus/logging/health", (Logger logger) =>
  {
      var metrics = logger.GetMetricsSnapshot();
      var healthy = metrics.WriteFailures == 0;
      return Results.Json(new { healthy, metrics });
  });
  ```

## Sample
```csharp
var logger = new Logger(new LoggerOptions
{
    ApplicationId = "APP",
    Environment = "prod",
    CustomTargets = new List<ITarget>
    {
        new AsyncTargetWrapper(new FileTarget("logs/app.log", 5_000_000, 5, false), bufferSize: 1000, metrics: new LoggingMetrics())
    }
});

var metrics = logger.GetMetricsSnapshot();
Console.WriteLine($"Written: {metrics.WrittenEntries}, Dropped: {metrics.DroppedEntries}");
```
