---
sidebar_position: 6
title: Security Module
description: Vulnerability scanning and secret detection for Primus SaaS.
---

# Security Module

The **Primus SaaS Security Module** provides comprehensive vulnerability scanning and secret detection for your applications. It is designed to be privacy-first, running entirely within your local environment or CI/CD pipeline without sending code to the cloud.

## Features

*   **Dependency Scanning**: Detects known vulnerabilities (CVEs) in your project dependencies.
*   **Secret Detection**: Finds hardcoded secrets (API keys, tokens, passwords) in your source code.
*   **Multi-Ecosystem**: Supports NuGet (.NET), NPM (Node.js), Python (pip), and Java (Maven).
*   **Hybrid Architecture**: Choose between a **Local Database** (default, private) or **Cloud API** (real-time).

## Installation

### NuGet Package
Install the core security library into your .NET project:

```bash
dotnet add package PrimusSaaS.Security
```

### CLI Tool
For CI/CD integration, use the standalone CLI tool `PrimusSecurityScanner`.

```bash
# Build the tool locally (for now)
dotnet build tools/PrimusSecurityScanner
```

## Usage

### Running the Scanner
Run the scanner against your project directory:

```bash
dotnet run --project tools/PrimusSecurityScanner -- --scan ./MyProject --db data/cve-database/cve-database.db
```

### Options
*   `--scan <path>`: Path to the project or directory to scan.
*   `--db <path>`: Path to the local CVE database (SQLite).
*   `--patterns <path>`: Path to the secret patterns JSON file (for secret detection).
*   `--update`: Updates the local CVE database (requires internet).
*   `--cloud-api-key <key>`: Uses the Cloud Provider instead of local DB.

## Supported Ecosystems

| Ecosystem | File Type | Description |
| :--- | :--- | :--- |
| **.NET** | `.csproj` | Scans `PackageReference` items. |
| **Node.js** | `package.json` | Scans `dependencies` and `devDependencies`. |
| **Python** | `requirements.txt` | Scans `package==version` entries. |
| **Java** | `pom.xml` | Scans Maven `dependency` entries. |

## CI/CD Integration

Add the scanner to your GitHub Actions workflow:

```yaml
- name: Run Primus Security Scan
  run: |
    dotnet run --project tools/PrimusSecurityScanner -- \
      --scan . \
      --db data/cve-database/cve-database.db
```

## Architecture

### Local Provider (Default)
*   **Privacy**: Your dependency list never leaves your machine.
*   **Speed**: Instant SQL queries against the local `cve-database.db`.
*   **Offline**: Works without internet access (once DB is downloaded).

### Cloud Provider
*   **Freshness**: Queries the Primus Cloud API for the latest vulnerability data.
*   **Usage**: Pass `--cloud-api-key YOUR_KEY` to enable.

## Secret Detection
The scanner checks for patterns defined in `SecretPatterns.json`. It detects:
*   AWS Access Keys
*   Azure Connection Strings
*   Private Keys (RSA, DSA, EC)
*   Generic API Tokens (Bearer, etc.)
