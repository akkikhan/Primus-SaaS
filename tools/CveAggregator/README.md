# CVE Data Aggregator

**Purpose**: Scrape and aggregate vulnerability data from multiple sources into a local SQLite database.

**Status**: 🚧 Scaffolding Complete (awaiting NVD API key)

---

## Features

- ✅ **Multi-Source Scraping**: NVD, GitHub Advisory, NuGet, NPM
- ✅ **Local Database**: SQLite with Zstandard compression
- ✅ **CLI Interface**: Easy-to-use command-line tool
- ✅ **Configurable**: API keys and settings via `appsettings.json`

---

## Prerequisites

### 1. NVD API Key (REQUIRED)

**Register here**: https://nvd.nist.gov/developers/request-an-api-key

1. Create account
2. Submit request
3. Wait for approval email (1-2 weeks)
4. Set environment variable:
   ```bash
   export NVD__ApiKey="your-api-key-here"
   ```

### 2. GitHub Token (Optional, for higher rate limits)

Generate a personal access token:
https://github.com/settings/tokens

Set environment variable:
```bash
export GitHub__Token="your-github-token"
```

---

## Installation

```bash
cd tools/CveAggregator
dotnet restore
dotnet build
```

---

## Usage

### Scrape CVE Data

```bash
# Scrape from all sources (last 30 days)
dotnet run -- scrape

# Scrape from specific sources
dotnet run -- scrape --sources nvd github --days 7

# Specify output path
dotnet run -- scrape --output /path/to/cve-database.db
```

### Build Database

```bash
# Build database from scraped data
dotnet run -- build

# Build without compression
dotnet run -- build --compress false

# Custom input/output paths
dotnet run -- build --input ./raw-data --output ./mydb.db
```

### Show Statistics

```bash
# Show database stats
dotnet run -- stats

# Stats for specific database
dotnet run -- stats --database /path/to/cve-database.db
```

---

## Configuration

Edit `appsettings.json`:

```json
{
  "NVD": {
    "ApiKey": "YOUR-API-KEY-HERE",
    "RateLimitDelay": 6000
  },
  "GitHub": {
    "Token": "YOUR-GITHUB-TOKEN"
  }
}
```

Or use environment variables:
- `NVD__ApiKey`
- `GitHub__Token`

---

## Implementation Status

### ✅ Complete

- [x] Project structure
- [x] CLI interface (System.CommandLine)
- [x] Dependency injection setup
- [x] Scraper interfaces
- [x] Database interface
- [x] Configuration system
- [x] Logging

### ⏳ TODO (Blocked by NVD API Key)

- [ ] NVD API implementation
- [ ] GitHub GraphQL API implementation
- [ ] NuGet API implementation
- [ ] NPM API implementation
- [ ] Database insertion logic
- [ ] Zstandard compression
- [ ] Statistics aggregation

---

## Architecture

```
CveAggregator
├── Program.cs                 # CLI entry point
├── CveAggregatorService.cs    # Orchestrator service
├── Scrapers/
│   ├── IScrapers.cs           # Scraper interfaces
│   ├── NvdScraper.cs          # NVD implementation
│   └── OtherScrapers.cs       # GitHub, NuGet, NPM
├── Database/
│   └── CveDatabase.cs         # SQLite wrapper
├── Models/
│   └── Vulnerability.cs       # Domain models
└── appsettings.json           # Configuration
```

---

## Next Steps

1. **CRITICAL**: Register for NVD API key (do this NOW!)
2. Implement NVD scraper (when API key arrives)
3. Implement other scrapers
4. Test with small dataset
5. Generate production database

---

## Example Output

```
🔒 Primus Security CVE Aggregator
==================================================
📥 Starting CVE data scraping...
   Sources: nvd, github, nuget, npm
   Days: 30
   Output: ../../../data/cve-database/cve-database.db

🌐 Scraping NVD for last 30 days...
✅ NVD scraping complete: 1,234 vulnerabilities

🌐 Scraping GitHub Advisory Database...
✅ GitHub Advisory scraping complete: 567 vulnerabilities

🌐 Scraping NuGet Security Advisories...
✅ NuGet scraping complete: 123 vulnerabilities

🌐 Scraping NPM Security Advisories...
✅ NPM scraping complete: 890 vulnerabilities

✅ Scraping complete!

🔨 Building CVE database...
   Input: raw-data/
   Output: cve-database.db
   Compress: true

✅ Database built successfully!
🗜️  Compressing database...
✅ Compression complete!

📊 CVE Database Statistics
   Database: cve-database.db

Total Vulnerabilities: 2,814
By Source:
  NVD: 1,234
  GitHub: 567
  NuGet: 123
  NPM: 890
```

---

**Status**: Ready to implement when NVD API key arrives! 🚀

**Last Updated**: December 4, 2025
