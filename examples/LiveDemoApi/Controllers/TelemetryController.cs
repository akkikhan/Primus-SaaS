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
    private readonly LiveDemoRuntimeState _runtimeState;

    public TelemetryController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<TelemetryController> logger,
        LiveDemoRuntimeState runtimeState)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _runtimeState = runtimeState;
    }

    [HttpGet("summary")]
    public IActionResult Summary()
    {
        var process = Process.GetCurrentProcess();
        var aiConnectionString = _configuration["PrimusLogging:ApplicationInsights:ConnectionString"];
        var aiConfigured = !string.IsNullOrWhiteSpace(aiConnectionString)
                           && aiConnectionString != "your-application-insights-connection-string";
        
        // Parse instrumentation key from connection string if available
        string? instrumentationKey = null;
        if (aiConfigured && aiConnectionString != null)
        {
            var parts = aiConnectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("InstrumentationKey=", StringComparison.OrdinalIgnoreCase))
                {
                    instrumentationKey = part.Substring("InstrumentationKey=".Length);
                    break;
                }
            }
        }

        var uptimeSpan = Uptime.Elapsed;
        var uptimeFormatted = uptimeSpan.TotalHours >= 1 
            ? $"{(int)uptimeSpan.TotalHours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s"
            : uptimeSpan.TotalMinutes >= 1 
                ? $"{(int)uptimeSpan.TotalMinutes}m {uptimeSpan.Seconds}s"
                : $"{uptimeSpan.Seconds}s";

        var payload = new
        {
            environment = _environment.EnvironmentName,
            timestamp = DateTime.UtcNow.ToString("o"),
            
            // Application Insights section (matches frontend expectations)
            applicationInsights = new
            {
                enabled = aiConfigured,
                instrumentationKey = instrumentationKey,
                portalUrl = aiConfigured ? "https://portal.azure.com/#blade/HubsExtension/BrowseResource/resourceType/microsoft.insights%2Fcomponents" : null
            },
            
            // Server section
            server = new
            {
                name = Environment.MachineName,
                uptime = new
                {
                    seconds = (int)uptimeSpan.TotalSeconds,
                    formatted = uptimeFormatted
                }
            },
            
            // Runtime section
            runtime = new
            {
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                processId = process.Id,
                threadCount = process.Threads.Count
            },
            
            // Memory section
            memory = new
            {
                workingSetMB = process.WorkingSet64 / 1024 / 1024,
                gcTotalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024
            },
            
            // Identity section
            identity = new
            {
                enabled = _runtimeState.IdentityEnabled,
                message = _runtimeState.IdentityMessage
            },
            
            // Logging section
            logging = new
            {
                applicationInsights = aiConfigured,
                logFile = Path.GetFullPath(_configuration["PrimusLogging:Targets:File:Path"] ?? "logs/livedemo-api.log", _environment.ContentRootPath)
            }
        };

        _logger.LogInformation("Telemetry summary requested: {@Payload}", payload);
        return Ok(payload);
    }
}
