# Performance & Resilience Hardening Plan

## Objectives
- Reduce allocations and serialization overhead while preserving safety.
- Verify behavior under load/soak and backpressure scenarios.
- Provide measurable budgets (latency, drops, CPU) and a repeatable benchmarking harness.

## Planned Improvements
- Introduce source-generated logging helpers (LoggerMessage) for the adapter layer where MEL is used, to avoid boxing and string formatting costs.
- Pool buffers for JSON serialization (ArrayPool-backed Utf8JsonWriter) inside SafeLogFormatter for heavy workloads.
- Tighten guards: enforce MaxContextBytes/MaxDepth/MaxEnumerableLength before formatting; short-circuit empty-context logs.
- Expand async buffering: configure bounded channels with drop-oldest and expose drop metrics (already in place) plus optional flushing hooks.

## Benchmarks
- Baseline: console + file targets, min level Info, steady-state 10k logs/sec.
- Scenarios: structured logging with 5 props, exceptions with inner exceptions, large strings near MaxStringLength, ClaimsPrincipal payloads.
- Metrics: median/p95/p99 per-log latency, CPU%, allocations/log, drop rate, file throughput (MB/s).
- Tools: BenchmarkDotNet microbenchmarks for formatter/serializer; K6 or simple worker harness for sustained logging; dotnet-trace/dotnet-counters for GC/allocs.

## Soak/Load Tests
- Duration: 1h soak at 5k logs/sec; spike to 20k logs/sec for 1 minute every 10 minutes.
- Validate: no crashes, bounded drops (configurable), file rotation intact, AI target non-blocking.
- Assertions: drops < 0.1% at steady-state; write failures = 0; no unhandled exceptions.

## Rollout Safeguards
- Feature flags: toggle async buffering and advanced serialization pooling separately.
- CI gates: run microbenchmarks (compare to baselines), unit tests, and smoke-load harness on PRs touching logging.
- Telemetry: emit metrics for drops/failures and formatter exceptions; expose health snapshot endpoint hook.

## Work Items
- [ ] Add source-generated logging helpers for PrimusLoggerAdapter paths that originate from MEL.
- [x] Add pooled Utf8JsonWriter support in SafeLogFormatter with size cap enforcement.
- [x] Add BenchmarkDotNet project with baseline and regression guardrails (initial benches).
- [x] Add soak/load harness (console app or test) with configurable RPS and targets.
- [ ] Wire CI to run benchmarks in compare mode (warn on regression).
