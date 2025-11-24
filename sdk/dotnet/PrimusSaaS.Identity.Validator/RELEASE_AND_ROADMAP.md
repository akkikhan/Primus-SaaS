# Release & Roadmap (Primus Identity Validator)

## Versioning
- Semantic Versioning (MAJOR.MINOR.PATCH).
- Breaking changes bump MAJOR; new features backward-compatible bump MINOR; fixes bump PATCH.

## Staged Rollout
1. Pre-release (alpha/beta) packages for new features (JWKS resiliency, diagnostics, rate limiting).
2. Internal canary using sample apps (Azure AD + LocalAuth).
3. General availability after canary validation and green CI.

## What’s Recently Added
- JWKS discovery normalization + retry/backoff with diagnostics.
- Config validation with actionable errors.
- Diagnostics endpoint helper (issuers/JWKS/security metrics).
- Rate limiting for failed validations.
- Security event logging hooks.

## Next Candidates
- Optional token lifecycle/refresh helper (or documented out-of-scope).
- Expanded sample/docs refresh for Azure AD end-to-end.
- Optional policy helper scaffolding (without claims mapping).
- Logging sink health/metrics endpoint guidance.

## Support Window
- Active: latest MAJOR and previous MAJOR receive fixes.
- Security fixes prioritized on latest release; backports case-by-case.
