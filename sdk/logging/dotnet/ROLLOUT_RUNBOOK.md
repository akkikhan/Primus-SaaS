# Logging Rollout & Monitoring Runbook

Goal: ship Primus Logging (with shim + bridges) safely across services. Follow canary → dual → cutover with explicit SLOs and rollback criteria.

## 1) Prereqs
- Ensure `PrimusLogging` config exists (ApplicationId/Environment/Targets, PII masking on).
- If using Serilog/NLog sinks, configure those stacks first; then add `Type = "serilog"` or `Type = "nlog"` target.
- Keep `UsePrimusLogging` middleware enabled to enrich request/user/tenant context.
- Feature flag: ability to disable Primus provider and fall back to legacy logging (config toggle).

## 2) SLOs to Watch
- Adapter error rate (shim path): `AdapterForwardedEntries` should increase; failures should stay at 0.
- Drops: `LoggingMetricsSnapshot.DroppedEntries` == 0; any increase triggers investigation.
- Write failures: `LoggingMetricsSnapshot.WriteFailures` == 0.
- Latency: request p99 should not regress by more than agreed budget (e.g., +2%).
- Volume: no >10% spike vs baseline when dual logging is on.

## 3) Canary
- Scope: 1–2 low-risk services.
- Enable Primus provider via config; keep legacy providers running (dual).
- Observe for 30–60 minutes:
  - Expose `/primus/logging/metrics` (see HEALTH_AND_METRICS.md).
  - Dashboard: Drops, WriteFailures, AdapterForwardedEntries, overall log volume.
- Rollback triggers:
  - Any drops > 0 after 5 minutes.
  - WriteFailures > 0 sustained for 2 minutes.
  - Error rate/latency outside budget.

## 4) Dual Logging Phase
- Keep both providers; add filters if duplication is noisy.
- If using Serilog/NLog bridge, verify sink delivery (Elasticsearch/Seq/Splunk/App Insights) matches legacy.
- Compare volumes between Primus and legacy provider for a sample endpoint.
- Keep feature flag to disable Primus provider quickly.

## 5) Cutover
- Clear legacy providers (`builder.Logging.ClearProviders();` then `AddPrimus(...)`).
- Keep the shim so `ILogger<T>` call sites continue to work.
- Monitor same SLOs for 24h.
- If stable, proceed to next service tier.

## 6) Optional Refactor to Primus Logger APIs
- Run converter in dry-run, review diffs, then apply with `--write`.
- Prioritize high-throughput services to remove adapter overhead.
- Monitor `AdapterForwardedEntries` trending toward zero as refactors complete.

## 7) Runbook for Incidents
- Symptoms: Drops > 0, WriteFailures > 0, missing logs in sinks.
- Immediate actions:
  1) Flip feature flag to disable Primus provider (fallback to legacy).
  2) If bridge is failing, disable the `serilog`/`nlog` target in config.
  3) Capture metrics snapshot and offending configs.
- Remediation:
  - Check sink connectivity and credentials.
  - Reduce async buffer size if process shutdown is dropping logs; ensure graceful shutdown flush.
  - Validate PII masking options didn’t block serialization.

## 8) Dashboards (suggested)
- Panel: WrittenEntries, DroppedEntries, WriteFailures, AdapterForwardedEntries (per service).
- Panel: Request latency/error rate (to catch perf regressions).
- Panel: Log volume per target (compare Primus vs legacy).
- Alert: Drops > 0 (critical), WriteFailures > 0 (warning/critical), AdapterForwardedEntries flatlined unexpectedly (indicates shim not being used or refactor done).

## 9) Smoke Checklist per Service
- [ ] `/primus/logging/metrics` reachable and returns zeros for drops/failures.
- [ ] Logs show requestId/correlationId/user/tenant as applicable.
- [ ] If bridging, entries appear in Serilog/NLog sinks with context intact.
- [ ] PII masking verified on a sample message.
- [ ] Feature flag toggle tested once.
