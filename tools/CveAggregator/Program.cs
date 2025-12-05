using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Security.CveAggregator.Scrapers;
using PrimusSaaS.Security.CveAggregator.Database;
using System.CommandLine;

namespace PrimusSaaS.Security.CveAggregator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        // Build service provider
        var services = new ServiceCollection()
            .AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .AddSingleton<IConfiguration>(configuration)
            .AddHttpClient()
            .AddSingleton<ICveDatabase, CveDatabase>()
            .AddSingleton<INvdScraper, NvdScraper>()
            .AddSingleton<IGitHubAdvisoryScraper, GitHubAdvisoryScraper>()
            .AddSingleton<INuGetAdvisoryScraper, NuGetAdvisoryScraper>()
            .AddSingleton<INpmAdvisoryScraper, NpmAdvisoryScraper>()
            .AddSingleton<CveAggregatorService>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🔒 Primus Security CVE Aggregator");
        logger.LogInformation("="+ new string('=', 50));

        // Define CLI commands
        var rootCommand = new RootCommand("CVE Data Aggregator for Primus Security Module");

        var scrapeCommand = new Command("scrape", "Scrape CVE data from sources")
        {
            new Option<string[]>("--sources", "Sources to scrape (nvd, github, nuget, npm)")
            {
                AllowMultipleArgumentsPerToken = true
            },
            new Option<int>("--days", () => 30, "Days of data to scrape"),
            new Option<string>("--output", () => "../../../data/cve-database/cve-database.db", "Output database path")
        };

        scrapeCommand.SetHandler(async (string[] sources, int days, string output) =>
        {
            var aggregator = services.GetRequiredService<CveAggregatorService>();
            await aggregator.ScrapeAsync(sources, days, output);
        }, 
        scrapeCommand.Options[0] as Option<string[]>, 
        scrapeCommand.Options[1] as Option<int>,
        scrapeCommand.Options[2] as Option<string>);

        var buildCommand = new Command("build", "Build CVE database from scraped data")
        {
            new Option<string>("--input", () => "../../../data/cve-database/raw/", "Input directory with scraped data"),
            new Option<string>("--output", () => "../../../data/cve-database/cve-database.db", "Output database path"),
            new Option<bool>("--compress", () => true, "Compress database with Zstandard")
        };

        buildCommand.SetHandler(async (string input, string output, bool compress) =>
        {
            var aggregator = services.GetRequiredService<CveAggregatorService>();
            await aggregator.BuildDatabaseAsync(input, output, compress);
        },
        buildCommand.Options[0] as Option<string>,
        buildCommand.Options[1] as Option<string>,
        buildCommand.Options[2] as Option<bool>);

        var statsCommand = new Command("stats", "Show CVE database statistics")
        {
            new Option<string>("--database", () => "../../../data/cve-database/cve-database.db", "Database path")
        };

        statsCommand.SetHandler(async (string database) =>
        {
            var aggregator = services.GetRequiredService<CveAggregatorService>();
            await aggregator.ShowStatsAsync(database);
        },
        statsCommand.Options[0] as Option<string>);

        rootCommand.AddCommand(scrapeCommand);
        rootCommand.AddCommand(buildCommand);
        rootCommand.AddCommand(statsCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
