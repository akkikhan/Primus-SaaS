# CI Pipeline Notes for PrimusSaaS.Logging

## Goals
- Prove safety and perf regressions before merge.
- Catch serialization/scope/sink regressions across TFMs.

## Recommended Steps
- Matrix: net6.0, net7.0, net8.0
  - `dotnet test sdk/logging/dotnet/PrimusSaaS.Logging.Tests/PrimusSaaS.Logging.Tests.csproj -f <TFM>`
- Load smoke (non-blocking but recommended):
  - `dotnet run -c Release --project sdk/logging/dotnet/PrimusSaaS.Logging.Benchmarks/PrimusSaaS.Logging.Benchmarks.csproj -- --load --rate 5000 --durationSeconds 60 --maxDrops 0`
- Benchmarks (informational, compare to baselines):
  - `dotnet run -c Release --project sdk/logging/dotnet/PrimusSaaS.Logging.Benchmarks/PrimusSaaS.Logging.Benchmarks.csproj`
  - Store/report BDN results for regression checks.
- Lint/format (if enabled): `dotnet format`

## Rollout Guardrails
- Enable health endpoint only in secured environments.
- Monitor metrics (drops/failures) during canary rollout; rollback by disabling Primus provider if issues arise.
