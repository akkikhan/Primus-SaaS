# PrimusSaaS.Logging Hardening Spec

## Expected Behaviors
- Never crash the host due to logging or serialization; failures must degrade gracefully and emit a fallback log.
- Safe serialization by default: ignore cycles, cap depth/collection size/string length, and convert unsupported types (Type, ClaimsPrincipal, HttpContext subsets, Exception) into safe shapes.
- Config validated at startup with actionable errors; invalid configurations fail fast.
- Multi-sink support (console/debug, file with rolling/compression, Application Insights, custom) with async buffering/backpressure (drop-oldest) that does not block request threads.
- Structured logging with scopes/correlation IDs and automatic application/environment enrichment.
- Observability-first: log/metric hooks for serialization failures, dropped log entries, sink health, and correlation presence.
- Scopes and correlation: ambient scopes flow via AsyncLocal; middleware sets request/correlation IDs and response headers; scopes merge into every log.
- Backpressure visibility: async buffering reports drop counts; metrics surface writes/failures/drops for health.
- Health endpoints: expose a simple health snapshot for targets/metrics and a diagnostics dump for recent serialization failures.

## Configuration Surface
- Required: `ApplicationId`, `Environment`.
- Logging: `MinLevel`, enrichers, scopes/correlation ID defaults.
- Serialization: `MaxDepth`, `MaxEnumerableLength`, `MaxStringLength`, `MaxContextBytes`, `IgnoreCycles`, safe converters enabled by default.
- Targets: type (`console`, `file`, `applicationInsights`, custom), async toggle, buffer size, file path/rotation/compression settings, AI connection string.
- Validation: ensure required fields are present, numeric limits are positive, and target-specific settings are coherent (e.g., file path required when `type=file`).

## Edge Cases (incl. Azure AD authority/issuer permutations)
- Authorities with trailing slashes, mixed casing, regional clouds, v1 vs v2 endpoints, single-tenant vs common/multi-tenant; normalize hosts and block `http`.
- Discovery/JWKS timeouts or throttling; retries with jitter and bounded timeouts.
- Rotated signing keys; cache invalidation and refresh.
- Claims payloads with unexpected/empty claims, multiple identities, or large graphs; log only safe, minimal slices.
- HttpContext/ClaimsPrincipal inclusion in logs must never introduce cycles or unsupported runtime types.

## Failure Modes & Handling
- Serialization error: emit sanitized `context` via fallback (ToString), increment failure metric, never throw.
- Sink write failure: circuit-breaker/backoff, fallback to console, surface health signal.
- Buffer saturation: drop-oldest policy with metric/log notice.
- Config invalid: fail at startup with actionable message (which setting, why).

## Acceptance Criteria
- Logging cannot crash the host under any provided state (ClaimsPrincipal, HttpContext, System.Type, large/cyclic graphs).
- Startup validation blocks bad configs with clear, actionable errors.
- Async buffering drops or flushes within policy; backpressure never blocks request pipeline.
- Correlation ID present on every request log when middleware is enabled.
- File rolling/compression works within size limits; AI sink accepts logs when configured.
- Tests cover serialization edge cases, config validation, sink write paths, async buffering, and regression cases from reported crashes.

## Delivery Loop
spec → design review → defensive implementation → exhaustive automated tests → observability hooks → docs + samples → staged rollout with monitoring.
