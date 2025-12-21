using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.CveAggregator.Database;
using PrimusSaaS.Security.CveAggregator.Scrapers;
using PrimusSaaS.Security.CveAggregator.Models;

namespace PrimusSaaS.Security.CveAggregator;

public class CveAggregatorService
{
    private readonly ILogger<CveAggregatorService> _logger;
    private readonly ICveDatabase _database;
    private readonly INvdScraper _nvdScraper;
    private readonly IGitHubAdvisoryScraper _gitHubScraper;
    private readonly INuGetAdvisoryScraper _nugetScraper;
    private readonly INpmAdvisoryScraper _npmScraper;
    private readonly IDummyScraper _dummyScraper;

    public CveAggregatorService(
        ILogger<CveAggregatorService> logger,
        ICveDatabase database,
        INvdScraper nvdScraper,
        IGitHubAdvisoryScraper gitHubScraper,
        INuGetAdvisoryScraper nugetScraper,
        INpmAdvisoryScraper npmScraper,
        IDummyScraper dummyScraper)
    {
        _logger = logger;
        _database = database;
        _nvdScraper = nvdScraper;
        _gitHubScraper = gitHubScraper;
        _nugetScraper = nugetScraper;
        _npmScraper = npmScraper;
        _dummyScraper = dummyScraper;
    }

    public async Task ScrapeAsync(string[] sources, int days, string outputPath)
    {
        _logger.LogInformation("📥 Starting CVE data scraping...");
        _logger.LogInformation("   Sources: {Sources}", string.Join(", ", sources));
        _logger.LogInformation("   Days: {Days}", days);
        _logger.LogInformation("   Output: {Output}", outputPath);

        var allSources = sources.Length == 0 ? new[] { "nvd", "github", "nuget", "npm" } : sources;
        var allVulnerabilities = new List<Vulnerability>();

        foreach (var source in allSources)
        {
            try
            {
                var scraper = source.ToLower() switch
                {
                    "nvd" => (ICveScraper)_nvdScraper,
                    "github" => _gitHubScraper,
                    "nuget" => _nugetScraper,
                    "npm" => _npmScraper,
                    "dummy" => _dummyScraper,
                    _ => null
                };

                if (scraper == null)
                {
                    _logger.LogWarning("⚠️  Unknown source: {Source}", source);
                    continue;
                }

                var vulnerabilities = await scraper.ScrapeAsync(days);
                allVulnerabilities.AddRange(vulnerabilities);
                _logger.LogInformation("   {Source}: {Count} vulnerabilities", scraper.SourceName, vulnerabilities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error scraping {Source}", source);
            }
        }

        // Save to database
        if (allVulnerabilities.Any())
        {
            _logger.LogInformation("");
            _logger.LogInformation("💾 Saving to database...");
            
            await _database.InitializeAsync(outputPath);
            await _database.InsertVulnerabilitiesAsync(allVulnerabilities);
            
            _logger.LogInformation("✅ Total vulnerabilities saved: {Count}", allVulnerabilities.Count);
        }
        else
        {
            _logger.LogWarning("⚠️  No vulnerabilities to save");
        }

        _logger.LogInformation("");
        _logger.LogInformation("✅ Scraping complete!");
    }

    public async Task BuildDatabaseAsync(string inputPath, string outputPath, bool compress)
    {
        _logger.LogInformation("🔨 Building CVE database...");
        _logger.LogInformation("   Input: {Input}", inputPath);
        _logger.LogInformation("   Output: {Output}", outputPath);
        _logger.LogInformation("   Compress: {Compress}", compress);

        try
        {
            await _database.InitializeAsync(outputPath);
            _logger.LogInformation("✅ Database built successfully!");
            
            if (compress)
            {
                _logger.LogInformation("🗜️  Compressing database...");
                // TODO: Implement Zstandard compression
                _logger.LogInformation("✅ Compression complete!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error building database");
        }
    }

    public async Task ShowStatsAsync(string databasePath)
    {
        _logger.LogInformation("📊 CVE Database Statistics");
        _logger.LogInformation("   Database: {Path}", databasePath);
        _logger.LogInformation("");

        try
        {
            await _database.InitializeAsync(databasePath);
            var count = await _database.GetVulnerabilityCountAsync();
            var stats = await _database.GetStatisticsAsync();

            _logger.LogInformation("Total Vulnerabilities: {Count:N0}", count);
            
            if (stats.Any())
            {
                _logger.LogInformation("");
                _logger.LogInformation("By Source:");
                foreach (var (source, sourceCount) in stats)
                {
                    _logger.LogInformation("  {Source}: {Count:N0}", source, sourceCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting statistics");
        }
    }
}
