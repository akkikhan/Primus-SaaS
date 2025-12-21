using PrimusSaaS.Security.CveAggregator.Models;

namespace PrimusSaaS.Security.CveAggregator.Scrapers;

/// <summary>
/// Base interface for all CVE scrapers
/// </summary>
public interface ICveScraper
{
    /// <summary>
    /// Scrape vulnerabilities from the source
    /// </summary>
    /// <param name="days">Number of days to scrape (0 = all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of vulnerabilities</returns>
    Task<List<Vulnerability>> ScrapeAsync(int days = 30, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Name of the scraper source
    /// </summary>
    string SourceName { get; }
}

/// <summary>
/// NVD (National Vulnerability Database) scraper
/// </summary>
public interface INvdScraper : ICveScraper
{
}

/// <summary>
/// GitHub Advisory Database scraper
/// </summary>
public interface IGitHubAdvisoryScraper : ICveScraper
{
}

/// <summary>
/// NuGet Security Advisories scraper
/// </summary>
public interface INuGetAdvisoryScraper : ICveScraper
{
}

/// <summary>
/// NPM Security Advisories scraper
/// </summary>
public interface INpmAdvisoryScraper : ICveScraper
{
}

/// <summary>
/// Dummy scraper for testing
/// </summary>
public interface IDummyScraper : ICveScraper
{
}
