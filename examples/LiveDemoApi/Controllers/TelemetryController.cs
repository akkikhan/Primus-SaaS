using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LiveDemoApi.Controllers;

[ApiController]
[Route("telemetry")]
public class TelemetryController : ControllerBase
{
    private static readonly Stopwatch Uptime = Stopwatch.StartNew();
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(IConfiguration configuration, IWebHostEnvironment environment, ILogger<TelemetryController> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("summary")]
    public IActionResult Summary()
    {
        var process = Process.GetCurrentProcess();
        var aiConfigured = !string.IsNullOrWhiteSpace(_configuration["PrimusLogging:ApplicationInsights:ConnectionString"])
                           && _configuration["PrimusLogging:ApplicationInsights:ConnectionString"] != "your-application-insights-connection-string";

        var payload = new
        {
            environment = _environment.EnvironmentName,
            uptimeSeconds = (int)Uptime.Elapsed.TotalSeconds,
            process = new
            {
                pid = process.Id,
                memoryMb = process.WorkingSet64 / 1024 / 1024,
                cpuTimeSeconds = (int)process.TotalProcessorTime.TotalSeconds,
                threads = process.Threads.Count
            },
            logging = new
            {
                applicationInsights = aiConfigured,
                logFile = Path.GetFullPath(_configuration["PrimusLogging:Targets:File:Path"] ?? "logs/livedemo-api.log", _environment.ContentRootPath)
            },
            timestamp = DateTime.UtcNow.ToString("o")
        };

        _logger.LogInformation("Telemetry summary requested: {@Payload}", payload);
        return Ok(payload);
    }
}
