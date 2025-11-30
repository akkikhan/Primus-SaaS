using LiveDemoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace LiveDemoApi.Controllers;

[ApiController]
[Route("log")]
public class LoggingController : ControllerBase
{
    private readonly ILogger<LoggingController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public LoggingController(ILogger<LoggingController> logger, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost("test")]
    public IActionResult Post(LogTestRequest request)
    {
        _logger.LogInformation("📝 Test log entry: Message={Message}, UserId={UserId}", request.Message ?? "Hello", request.UserId ?? "anonymous");
        return Ok(new
        {
            logged = true,
            message = request.Message ?? "Hello",
            level = request.Level ?? "Information",
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }

    [HttpGet("/logs/recent")]
    public IActionResult Tail()
    {
        var relativePath = _configuration["PrimusLogging:Targets:File:Path"] ?? "logs/livedemo-api.log";
        var path = Path.GetFullPath(relativePath, _environment.ContentRootPath);
        if (!System.IO.File.Exists(path))
        {
            return Ok(new { file = path, size = 0, tail = "Log file not found yet. Generate activity to create it." });
        }

        var info = new FileInfo(path);
        string tailContent;
        using (var stream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            const int maxBytes = 8000;
            if (stream.Length > maxBytes)
            {
                stream.Seek(-maxBytes, SeekOrigin.End);
            }
            using var reader = new StreamReader(stream);
            tailContent = reader.ReadToEnd();
        }

        return Ok(new
        {
            file = path,
            size = info.Length,
            tail = tailContent
        });
    }
}
