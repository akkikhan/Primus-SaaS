# Rollout & CI Checklist for PrimusSaaS.Logging

## Pre-merge
- [ ] All tests green: `dotnet test sdk/logging/dotnet/PrimusSaaS.Logging.Tests/PrimusSaaS.Logging.Tests.csproj`.
- [ ] Config validation exercised (ApplicationId/Environment/targets/serialization bounds).
- [ ] Health snapshot verified in a sample app (metrics + targets list).
- [ ] LoggingMiddleware verified to emit X-Request-ID and X-Correlation-ID.
- [ ] Metrics exposed: drops, write failures, writes.
- [ ] Documentation updated for any new options/behaviors.

## CI Gates
- Run tests across TFMs: net6.0, net7.0, net8.0 (matrix).
- Optional/Recommended: run BenchmarkDotNet suite (compare to baseline; warn on regression). Use load harness with `--load --rate 5000 --durationSeconds 60 --maxDrops 0` for a smoke soak.
- Lint/format: dotnet format (if enabled).
- Document failures: capture metrics snapshot and health endpoint output on CI failure for triage.

## Staged Rollout
- Enable in canary environment with async buffering ON and health endpoint wired.
- Monitor metrics: drops, write failures, latency, AI target success.
- Gradually increase traffic; rollback plan: disable Primus provider and fall back to built-in console/debug providers.

## Post-deploy Monitoring
- Check health snapshot endpoint/output for target health.
- Track correlation ID flow across services.
- Watch for buffer saturation (drops) and sink failures.
- Review log sizes vs MaxContextBytes enforcement.

## Documentation Alignment
- Ensure sample configs include: ApplicationId/Environment, targets, serialization limits, async buffering, correlation middleware.
- Troubleshooting section: serialization fallback behavior, buffer drops, AI connectivity failures, file rotation issues.
