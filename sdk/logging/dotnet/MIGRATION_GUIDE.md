# PrimusSaaS.Logging Migration Guide (Microsoft.Extensions.Logging → Primus)

This guide shows how to move existing `ILogger<T>` call sites onto Primus Logging with minimal churn. Start with the compatibility shim (no call-site changes), then optionally migrate to direct `Logger` APIs when ready.

## 1) Quick Win: Compatibility Shim (No Code Changes)

**What:** Keep `ILogger<T>` in controllers/services; Primus runs underneath.

**How:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(builder.Configuration.GetSection("PrimusLogging"));
// optional middleware for HTTP enrichment
builder.Services.AddPrimusLogging(builder.Configuration.GetSection("PrimusLogging"));
var app = builder.Build();
app.UsePrimusLogging();
```

**Verify:**
- Run tests: `dotnet test sdk/logging/dotnet/PrimusSaaS.Logging.Tests`
- Hit an endpoint and check logs include `applicationId/environment` + request/correlation ids (from middleware).
- Inspect metrics: `logger.GetMetricsSnapshot().AdapterForwardedEntries` should increment when using `ILogger<T>`.

## 2) Dual Logging (Transition Phase)

**What:** Run Primus + existing providers side-by-side, then retire legacy providers.

**How:**
- Keep existing providers; add Primus via `AddPrimus(...)` (do **not** clear providers initially).
- Add filters to avoid double-writing the same category if needed.
- Monitor volume to ensure no duplication storms.

**Verify/Observe:**
- Monitor Primus metrics (`WrittenEntries`, `DroppedEntries`, `WriteFailures`, `AdapterForwardedEntries`).
- Compare log volume between providers for a few endpoints.
- Add health endpoints (example in `HEALTH_AND_METRICS.md`).

## 3) Full Migration to Primus Logger APIs (Optional)

**What:** Replace `ILogger<T>` call sites with `PrimusSaaS.Logging.Core.Logger` methods (`Info/Warn/Error/...`).

**Suggested steps:**
1. Swap DI injection per class (constructor parameter) from `ILogger<T>` to `PrimusSaaS.Logging.Core.Logger`.
2. Replace method calls:
   - `LogInformation` → `Info`
   - `LogWarning` → `Warn`
   - `LogError` → `Error`
   - `LogCritical` → `Critical`
   - `LogTrace/LogDebug` → `Debug`
3. Keep structured logging arguments; Primus will capture them as context.
4. If you need category, add a scope once in the constructor:
   ```csharp
   _logger = logger;
   _logger = logger; // DI provides singleton Primus logger
   _categoryScope = _logger.BeginScope(new Dictionary<string, object?> { ["category"] = typeof(MyService).Name });
   ```
   Dispose the scope when the service is disposed.

**Automated assistance:**
- Use the dry-run converter to see safe edits:  
  `dotnet run --project ./PrimusSaaS.Logging.CallsiteConverter/PrimusSaaS.Logging.CallsiteConverter.csproj -- --path <root>`  
  Add `--write` to apply edits. Tool only rewrites calls with string-literal messages and reports skips; review diffs.

**Verify:**
- Build the project (should have no `ILogger<T>` injections left in converted files).
- Ensure contextual fields still appear (user/tenant/request via middleware scopes).
- Run targeted tests for converted services/controllers.

## 4) Observability Checklist

- Metrics: expose `logger.GetMetricsSnapshot()` via an endpoint or custom exporter.
- Health: use `logger.GetHealthSnapshot()` for target status + metrics.
- Logs: check that PII masking and safe serialization remain intact (Primus handles this by default).
- Shim metric `AdapterForwardedEntries` should drop as you finish converting call sites.

## 5) Rollout Pattern

1. **Canary:** Enable Primus shim on 1–2 services; monitor error rate and latency.
2. **Dual:** Add Primus alongside legacy providers; verify no duplication storms.
3. **Cutover:** Clear legacy providers; keep shim-enabled `ILogger<T>` to minimize code churn.
4. **Optional refactor:** Convert hot-path services to direct `Logger` APIs.
5. **Steady state:** Leave Primus as sole provider; keep a rollback toggle via config if desired.

**Need Serilog/NLog sinks?** Add `Type = "serilog"` or `Type = "nlog"` targets and configure those stacks as usual. Primus will forward enriched entries (PII-masked) into your existing sinks.

## 6) Configuration Snippet (appsettings.json)

```json
{
  "PrimusLogging": {
    "ApplicationId": "my-app",
    "Environment": "production",
    "MinLevel": 1,
    "Targets": [
      { "Type": "console", "Pretty": true },
      { "Type": "file", "Path": "logs/app.log", "Async": true }
    ],
    "Pii": { "MaskEmails": true, "MaskCreditCards": true, "MaskSSN": true }
  }
}
```

## 7) Safety Nets

- **Default to dry-runs** when doing automated edits; review diffs.
- Keep Primus middleware on to ensure request/user context enrichment.
- Use `LoggingMetricsSnapshot` to detect drops or failures before widening rollout.

## 8) Support Matrix

- .NET 6/7/8 supported.
- Works with ASP.NET Core pipeline via `UsePrimusLogging` middleware.
- Adapter covers `ILogger` scopes, event IDs, structured templates, and level mapping.
