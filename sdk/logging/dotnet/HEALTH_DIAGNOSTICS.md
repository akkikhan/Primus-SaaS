# Primus Logging Health & Diagnostics

## Health Endpoint (opt-in)
- Map with `endpoints.MapPrimusLoggingHealth("/_primus/logging/health");`
- Response includes metrics (writes/drops/failures) and target list.
- Use behind auth in production.

## Metrics to Watch
- `DroppedEntries`: backpressure events; investigate buffer sizing or target latency.
- `WriteFailures`: sink errors; check AI connectivity or file permissions.
- `WrittenEntries`: throughput indicator.

## Correlation
- Middleware sets `X-Request-ID` and `X-Correlation-ID` on responses and scopes; ensure upstreams propagate `X-Correlation-ID`.

## Troubleshooting
- High drops: increase buffer or enable async targets; check sink latency.
- Write failures: check sink creds/connectivity; fallback to console.
- Large payloads: adjust `Serialization.MaxContextBytes/MaxDepth/MaxEnumerableLength`.
