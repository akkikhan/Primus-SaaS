# PrimusSaaS.Logging Benchmarks

## Modes
- BenchmarkDotNet (default): `dotnet run -c Release`
- Load harness: `dotnet run -c Release -- --load --rate 5000 --durationSeconds 60`

## Benchmarks
- `LoggingBenchmarks`: end-to-end Info logging with context to a null target.
- `FormatterBenchmarks`: SafeLogFormatter serialization performance with pooled buffers.
- `AsyncFileTargetBenchmarks`: async wrapper + file target write throughput.
- `AsyncChannelBenchmarks`: bounded channel TryWrite throughput (drop-oldest policy).
- `AsyncTargetLoadHarness`: async target buffering performance at different buffer sizes.
- `ApplicationInsightsBenchmarks`: AI target write overhead (serialization focus).
- `FileRotationBenchmarks`: file target rotation under small size limits.

## Notes
- Run in Release for meaningful results.
- Load harness is a simple smoke soak; extend with metrics/assertions as needed.
- CI suggestion: run `--load` with `--maxDrops 0 --maxFailures 0` as a gating smoke, and run full BDN suite for perf baselines.
