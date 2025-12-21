# Changelog

All notable changes to PrimusSaaS.Security will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-preview.1] - 2025-12-21

### Added
- **Core Security Scanner** (`ISecurityScanner`) - Unified interface for all scanning operations
- **Secret Detection** - Regex + entropy-based detection with 30 built-in patterns
  - AWS, GitHub, Stripe, Google, Slack, Azure, and more
  - Configurable entropy thresholds to reduce false positives
- **Dependency Scanning** - Local CVE database lookup for vulnerable packages
  - Supports NuGet (.csproj), npm (package.json), pip (requirements.txt), Maven (pom.xml)
  - Semver range matching for accurate vulnerability detection
- **Policy Engine** - Configurable actions (Block/Warn/Audit/Ignore) per severity level
  - Allowlists for known-safe rules, CVEs, and packages
- **PDF Reporting** - Professional security reports via QuestPDF
- **Roslyn Analyzers** (compile-time)
  - PS0001: SQL Injection detection
  - PS0002: XSS detection (Html.Raw, innerHTML)
  - PS0003: Hardcoded secret constants
- **Data Isolation Verification** - Runtime check to confirm no network dependencies
- **DI Extensions** - `AddPrimusSecurity()` for easy ASP.NET Core integration

### Security
- **Zero external API calls** - All processing happens locally
- **No cloud dependencies** - Fully self-contained, offline-capable
- **Compile-time blocked** - Network assemblies are prohibited in project config

### Notes
- This is a **preview release** - APIs may change before 1.0.0 GA
- CVE database must be provided by the user (see README for instructions)
- Static analysis (Roslyn) runs at compile-time, not via `ScanAsync()`

## [Unreleased]

### Planned
- Runtime static analysis integration
- SARIF output format
- GitHub Actions integration
- Additional compliance standards (SOC2, HIPAA)
