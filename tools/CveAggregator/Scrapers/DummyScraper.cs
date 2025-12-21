using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.CveAggregator.Models;

namespace PrimusSaaS.Security.CveAggregator.Scrapers;

public class DummyScraper : IDummyScraper
{
    private readonly ILogger<DummyScraper> _logger;

    public DummyScraper(ILogger<DummyScraper> logger)
    {
        _logger = logger;
    }

    public string SourceName => "dummy";

    public Task<List<Vulnerability>> ScrapeAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating dummy vulnerability data...");

        var vulns = new List<Vulnerability>();

        // 1. Newtonsoft.Json Vulnerability (Classic High Severity)
        // Affected: < 13.0.1
        vulns.Add(new Vulnerability
        {
            Id = "GHSA-5crp-9r3c-p9vr",
            CveId = "CVE-2024-FAKE1",
            Description = "Improper handling of highly recursive structures in Newtonsoft.Json can lead to Denial of Service.",
            Severity = "HIGH",
            CVSSv3Score = 7.5,
            CVSSv3Vector = "CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:N/I:N/A:H",
            Source = "dummy",
            PublishedDate = DateTime.UtcNow.AddDays(-10),
            References = new List<string> { "https://github.com/StartAutomating/Newtonsoft.Json" },
            AffectedPackages = new List<AffectedPackage>
            {
                new AffectedPackage
                {
                    Ecosystem = "nuget",
                    PackageName = "Newtonsoft.Json",
                    AffectedVersionRange = "(,13.0.1)", // Less than 13.0.1
                    FixedInVersions = new List<string> { "13.0.1" }
                }
            }
        });

        // 2. Log4Net Vulnerability (CRITICAL)
        // Affected: < 2.0.10
        vulns.Add(new Vulnerability
        {
            Id = "GHSA-2qrg-x229-3v8q",
            CveId = "CVE-2024-FAKE2",
            Description = "XML External Entity (XXE) vulnerability in log4net when parsing XML configuration files.",
            Severity = "CRITICAL",
            CVSSv3Score = 9.8,
            CVSSv3Vector = "CVSS:3.1/AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H",
            Source = "dummy",
            PublishedDate = DateTime.UtcNow.AddDays(-5),
            References = new List<string> { "https://logging.apache.org/log4net/" },
            AffectedPackages = new List<AffectedPackage>
            {
                new AffectedPackage
                {
                    Ecosystem = "nuget",
                    PackageName = "log4net",
                    AffectedVersionRange = "(,2.0.10)",
                    FixedInVersions = new List<string> { "2.0.10" }
                }
            }
        });

        // 3. Lodash (NPM)
        // Affected: < 4.17.21
        vulns.Add(new Vulnerability
        {
            Id = "GHSA-35jh-r3h4-6jhm",
            CveId = "CVE-2024-FAKE3",
            Description = "Command Injection in lodash via template function.",
            Severity = "HIGH",
            CVSSv3Score = 7.2,
            Source = "dummy",
            PublishedDate = DateTime.UtcNow.AddDays(-20),
            AffectedPackages = new List<AffectedPackage>
            {
                new AffectedPackage
                {
                    Ecosystem = "npm",
                    PackageName = "lodash",
                    AffectedVersionRange = "<4.17.21", 
                    FixedInVersions = new List<string> { "4.17.21" }
                }
            }
        });

        return Task.FromResult(vulns);
    }
}
