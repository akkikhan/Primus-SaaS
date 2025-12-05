using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.CveAggregator.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrimusSaaS.Security.CveAggregator.Scrapers;

/// <summary>
/// NVD (National Vulnerability Database) scraper
/// API Docs: https://nvd.nist.gov/developers/vulnerabilities
/// </summary>
public class NvdScraper : INvdScraper
{
    private readonly ILogger<NvdScraper> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _apiKey;
    private readonly string _baseUrl;
    private readonly int _rateLimitDelay;

    public string SourceName => "NVD";

    public NvdScraper(
        ILogger<NvdScraper> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["NVD:ApiKey"];
        _baseUrl = configuration["NVD:BaseUrl"] ?? "https://services.nvd.nist.gov/rest/json/cves/2.0";
        _rateLimitDelay = int.Parse(configuration["NVD:RateLimitDelay"] ?? "6000");
    }

    public async Task<List<Vulnerability>> ScrapeAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🌐 Scraping NVD for last {Days} days...", days);

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("⚠️  NVD API Key not configured.");
            _logger.LogWarning("   Register at: https://nvd.nist.gov/developers/request-an-api-key");
            _logger.LogWarning("   Set environment variable: NVD__ApiKey=your-key-here");
            return new List<Vulnerability>();
        }

        var vulnerabilities = new List<Vulnerability>();

        try
        {
            // Calculate date range
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            var startDateStr = startDate.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            var endDateStr = endDate.ToString("yyyy-MM-ddTHH:mm:ss.fff");

            _logger.LogInformation("   Date range: {Start} to {End}", startDateStr, endDateStr);

            var httpClient = _httpClientFactory.CreateClient();
            
            // NVD API 2.0 requires API key in header
            httpClient.DefaultRequestHeaders.Add("apiKey", _apiKey);

            var startIndex = 0;
            var resultsPerPage = 2000; // NVD max
            var totalResults = 0;

            do
            {
                // Build request URL
                var url = $"{_baseUrl}?" +
                          $"pubStartDate={startDateStr}&" +
                          $"pubEndDate={endDateStr}&" +
                          $"resultsPerPage={resultsPerPage}&" +
                          $"startIndex={startIndex}";

                _logger.LogInformation("   Fetching page: start={Start}, limit={Limit}", startIndex, resultsPerPage);

                // Make API request
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("   ❌ API request failed: {Status} {Reason}", 
                        response.StatusCode, response.ReasonPhrase);
                    break;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var nvdResponse = JsonSerializer.Deserialize<NvdResponse>(json);

                if (nvdResponse == null)
                {
                    _logger.LogError("   ❌ Failed to parse NVD response");
                    break;
                }

                totalResults = nvdResponse.TotalResults;
                _logger.LogInformation("   ✅ Received {Count} vulnerabilities (total: {Total})", 
                    nvdResponse.Vulnerabilities?.Count ?? 0, totalResults);

                // Transform NVD format to our format
                if (nvdResponse.Vulnerabilities != null)
                {
                    foreach (var nvdItem in nvdResponse.Vulnerabilities)
                    {
                        try
                        {
                            var vulnerability = TransformNvdItem(nvdItem);
                            if (vulnerability != null)
                            {
                                vulnerabilities.Add(vulnerability);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("   ⚠️  Failed to transform CVE {Id}: {Error}", 
                                nvdItem.Cve?.Id, ex.Message);
                        }
                    }
                }

                startIndex += resultsPerPage;

                // Rate limiting: wait before next request
                if (startIndex < totalResults)
                {
                    _logger.LogInformation("   ⏳ Rate limit delay: {Delay}ms", _rateLimitDelay);
                    await Task.Delay(_rateLimitDelay, cancellationToken);
                }

            } while (startIndex < totalResults);

            _logger.LogInformation("✅ NVD scraping complete: {Count} vulnerabilities", vulnerabilities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error scraping NVD");
        }

        return vulnerabilities;
    }


    private Vulnerability TransformNvdItem(NvdVulnerabilityItem item)
    {
        if (item.Cve == null) return new Vulnerability();

        var cve = item.Cve;

        // Get English description
        var description = cve.Descriptions?
            .FirstOrDefault(d => d.Lang == "en")?.Value ?? "No description available";

        // Get CVSS score (prefer v3.1, then v3.0, then v2.0)
        double? cvssScore = null;
        string? cvssVector = null;

        if (cve.Metrics?.CvssMetricV31?.Any() == true)
        {
            var metric = cve.Metrics.CvssMetricV31.First();
            cvssScore = metric.CvssData?.BaseScore;
            cvssVector = metric.CvssData?.VectorString;
        }
        else if (cve.Metrics?.CvssMetricV30?.Any() == true)
        {
            var metric = cve.Metrics.CvssMetricV30.First();
            cvssScore = metric.CvssData?.BaseScore;
            cvssVector = metric.CvssData?.VectorString;
        }
        else if (cve.Metrics?.CvssMetricV2?.Any() == true)
        {
            var metric = cve.Metrics.CvssMetricV2.First();
            cvssScore = metric.CvssData?.BaseScore;
            cvssVector = metric.CvssData?.VectorString;
        }

        // Determine severity
        var severity = cvssScore.HasValue ? GetSeverity(cvssScore.Value) : "UNKNOWN";

        // Get CWE
        var cweId = cve.Weaknesses?
            .SelectMany(w => w.Description ?? new List<CweDescription>())
            .FirstOrDefault(d => d.Lang == "en" && d.Value?.StartsWith("CWE-") == true)?.Value;

        // Get references
        var references = cve.References?
            .Select(r => r.Url ?? string.Empty)
            .Where(url => !string.IsNullOrEmpty(url))
            .ToList() ?? new List<string>();

        // Extract Affected Packages from CPE
        var affectedPackages = new List<AffectedPackage>();
        if (cve.Configurations != null)
        {
            foreach (var config in cve.Configurations)
            {
                if (config.Nodes != null)
                {
                    foreach (var node in config.Nodes)
                    {
                        if (node.CpeMatch != null)
                        {
                            foreach (var match in node.CpeMatch)
                            {
                                if (match.Vulnerable)
                                {
                                    var package = ParseCpe(match);
                                    if (package != null)
                                    {
                                        affectedPackages.Add(package);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return new Vulnerability
        {
            Id = Guid.NewGuid().ToString(),
            CveId = cve.Id ?? "UNKNOWN",
            Description = description,
            CVSSv3Score = cvssScore,
            CVSSv3Vector = cvssVector,
            Severity = severity,
            PublishedDate = cve.Published,
            LastModified = cve.LastModified,
            CWE = cweId,
            Source = "NVD",
            References = references,
            AffectedPackages = affectedPackages
        };
    }

    private AffectedPackage? ParseCpe(CpeMatch match)
    {
        if (string.IsNullOrEmpty(match.Criteria)) return null;

        var package = new AffectedPackage();
        
        // Parse CPE string
        var parts = match.Criteria.Split(':');
        if (parts.Length >= 5)
        {
            // cpe:2.3:part:vendor:product:version...
            package.Ecosystem = "nvd"; 
            package.PackageName = $"{parts[3]}:{parts[4]}"; // vendor:product
            
            // Construct version range
            var version = parts.Length > 5 ? parts[5] : "*";
            if (version == "*" || version == "-")
            {
                var rangeParts = new List<string>();
                if (!string.IsNullOrEmpty(match.VersionStartIncluding)) rangeParts.Add($">={match.VersionStartIncluding}");
                if (!string.IsNullOrEmpty(match.VersionStartExcluding)) rangeParts.Add($">{match.VersionStartExcluding}");
                if (!string.IsNullOrEmpty(match.VersionEndIncluding)) rangeParts.Add($"<={match.VersionEndIncluding}");
                if (!string.IsNullOrEmpty(match.VersionEndExcluding)) rangeParts.Add($"<{match.VersionEndExcluding}");
                
                package.AffectedVersionRange = rangeParts.Any() ? string.Join(" ", rangeParts) : "*";
            }
            else
            {
                package.AffectedVersionRange = version;
            }
        }
        else
        {
            return null;
        }
        
        return package;
    }

    private static string GetSeverity(double cvssScore)
    {
        return cvssScore switch
        {
            >= 9.0 => "CRITICAL",
            >= 7.0 => "HIGH",
            >= 4.0 => "MEDIUM",
            > 0.0 => "LOW",
            _ => "UNKNOWN"
        };
    }
}

#region NVD API Response Models

public class NvdResponse
{
    [JsonPropertyName("resultsPerPage")]
    public int ResultsPerPage { get; set; }

    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; }

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("vulnerabilities")]
    public List<NvdVulnerabilityItem>? Vulnerabilities { get; set; }
}

public class NvdVulnerabilityItem
{
    [JsonPropertyName("cve")]
    public NvdCve? Cve { get; set; }
}

public class NvdCve
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("sourceIdentifier")]
    public string? SourceIdentifier { get; set; }

    [JsonPropertyName("published")]
    public DateTime? Published { get; set; }

    [JsonPropertyName("lastModified")]
    public DateTime? LastModified { get; set; }

    [JsonPropertyName("vulnStatus")]
    public string? VulnStatus { get; set; }

    [JsonPropertyName("descriptions")]
    public List<Description>? Descriptions { get; set; }

    [JsonPropertyName("metrics")]
    public Metrics? Metrics { get; set; }

    [JsonPropertyName("weaknesses")]
    public List<Weakness>? Weaknesses { get; set; }

    [JsonPropertyName("references")]
    public List<Reference>? References { get; set; }

    [JsonPropertyName("configurations")]
    public List<Configuration>? Configurations { get; set; }
}

public class Description
{
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class Metrics
{
    [JsonPropertyName("cvssMetricV31")]
    public List<CvssMetric>? CvssMetricV31 { get; set; }

    [JsonPropertyName("cvssMetricV30")]
    public List<CvssMetric>? CvssMetricV30 { get; set; }

    [JsonPropertyName("cvssMetricV2")]
    public List<CvssMetric>? CvssMetricV2 { get; set; }
}

public class CvssMetric
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("cvssData")]
    public CvssData? CvssData { get; set; }

    [JsonPropertyName("baseSeverity")]
    public string? BaseSeverity { get; set; }

    [JsonPropertyName("exploitabilityScore")]
    public double? ExploitabilityScore { get; set; }

    [JsonPropertyName("impactScore")]
    public double? ImpactScore { get; set; }
}

public class CvssData
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("vectorString")]
    public string? VectorString { get; set; }

    [JsonPropertyName("baseScore")]
    public double? BaseScore { get; set; }

    [JsonPropertyName("baseSeverity")]
    public string? BaseSeverity { get; set; }
}

public class Weakness
{
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("description")]
    public List<CweDescription>? Description { get; set; }
}

public class CweDescription
{
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class Reference
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

public class Configuration
{
    [JsonPropertyName("nodes")]
    public List<Node>? Nodes { get; set; }
}

public class Node
{
    [JsonPropertyName("operator")]
    public string? Operator { get; set; }

    [JsonPropertyName("negate")]
    public bool Negate { get; set; }

    [JsonPropertyName("cpeMatch")]
    public List<CpeMatch>? CpeMatch { get; set; }
}

public class CpeMatch
{
    [JsonPropertyName("vulnerable")]
    public bool Vulnerable { get; set; }

    [JsonPropertyName("criteria")]
    public string? Criteria { get; set; }

    [JsonPropertyName("matchCriteriaId")]
    public string? MatchCriteriaId { get; set; }
    
    [JsonPropertyName("versionStartIncluding")]
    public string? VersionStartIncluding { get; set; }
    
    [JsonPropertyName("versionEndIncluding")]
    public string? VersionEndIncluding { get; set; }
    
    [JsonPropertyName("versionStartExcluding")]
    public string? VersionStartExcluding { get; set; }
    
    [JsonPropertyName("versionEndExcluding")]
    public string? VersionEndExcluding { get; set; }
}

#endregion
