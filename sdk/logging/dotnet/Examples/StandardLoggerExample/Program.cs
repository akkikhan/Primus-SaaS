using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);
// Add services
builder.Services.AddControllers();
// Configure logging with Primus
builder.Logging.ClearProviders(); // Remove default console logger
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "STANDARD-LOGGER-EXAMPLE";
    options.Environment = builder.Environment.EnvironmentName.ToLowerInvariant();
    options.MinLevel = builder.Environment.IsDevelopment() ? PrimusSaaS.Logging.Core.LogLevel.Debug : PrimusSaaS.Logging.Core.LogLevel.Info;
    options.Targets = new List<PrimusSaaS.Logging.Core.TargetConfig>
    {
        new()
        {
            Type = "console",
            Pretty = true
        },
        new()
        {
            Type = "file",
            Path = "logs/app.log",
            Async = true,
            MaxFileSize = 5 * 1024 * 1024, // 5MB
            MaxRetainedFiles = 3,
            CompressRotatedFiles = true
        }
    };
    // Enable PII masking
    options.Pii.MaskEmails = true;
    options.Pii.MaskCreditCards = true;
    // Add custom enrichers
    options.Enrichers.Add(new PrimusSaaS.Logging.Core.MachineNameEnricher());
});
var app = builder.Build();
app.MapControllers();
app.Run();
// Example Controller using standard ILogger
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;
    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        _logger.LogInformation("Fetching all products");
        // Structured logging
        _logger.LogInformation("User {UserId} requested products from {IpAddress}", "user-123", HttpContext.Connection.RemoteIpAddress);
        return Ok(new[] { "Product1", "Product2" });
    }

    [HttpPost]
    public IActionResult Create([FromBody] string productName)
    {
        try
        {
            _logger.LogInformation("Creating product: {ProductName}", productName);
            // Simulate work
            if (string.IsNullOrEmpty(productName))
            {
                _logger.LogWarning("Product creation failed: empty name");
                return BadRequest("Product name is required");
            }

            _logger.LogInformation("Product created successfully: {ProductName}", productName);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create product");
            return StatusCode(500);
        }
    }
}
