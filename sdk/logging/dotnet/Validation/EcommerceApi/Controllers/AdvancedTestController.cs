using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Identity.Validator;

namespace EcommerceApi.Controllers;

/// <summary>
/// Controller to test advanced logging and identity features
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdvancedTestController : ControllerBase
{
    private readonly Logger _logger;

    public AdvancedTestController(Logger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test exception logging with stack traces
    /// </summary>
    [HttpPost("test-exception")]
    [AllowAnonymous]
    public IActionResult TestExceptionLogging([FromBody] ExceptionTestRequest request)
    {
        var correlationId = _logger.GenerateCorrelationId();
        
        _logger.Info("Starting exception test", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["exceptionType"] = request.ExceptionType
        });

        try
        {
            // Simulate different exception types
            switch (request.ExceptionType?.ToLower())
            {
                case "nullreference":
                    string? nullString = null;
                    _ = nullString!.Length; // Will throw NullReferenceException
                    break;
                    
                case "dividebyzero":
                    int x = 1;
                    int y = 0;
                    _ = x / y; // Will throw DivideByZeroException
                    break;
                    
                case "argumentnull":
                    throw new ArgumentNullException("testParameter", "This parameter cannot be null");
                    
                case "invalidoperation":
                    throw new InvalidOperationException("This operation is not valid in the current state");
                    
                case "nested":
                    try
                    {
                        throw new IOException("Inner exception: File not found");
                    }
                    catch (Exception inner)
                    {
                        throw new ApplicationException("Outer exception: Failed to process file", inner);
                    }
                    
                default:
                    throw new Exception($"Test exception of type: {request.ExceptionType ?? "generic"}");
            }
            
            return Ok(new { message = "No exception thrown" });
        }
        catch (Exception ex)
        {
            // Test the Error(Exception, message, context) overload
            _logger.Error(ex, "Exception caught during test", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["exceptionType"] = ex.GetType().Name,
                ["hasInnerException"] = ex.InnerException != null
            });

            return Ok(new
            {
                message = "Exception logged successfully",
                correlationId,
                exceptionType = ex.GetType().Name,
                exceptionMessage = ex.Message,
                hasStackTrace = !string.IsNullOrEmpty(ex.StackTrace),
                innerException = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Test all log levels with exception
    /// </summary>
    [HttpPost("test-all-levels-with-exception")]
    [AllowAnonymous]
    public IActionResult TestAllLevelsWithException()
    {
        var correlationId = _logger.GenerateCorrelationId();
        var testException = new InvalidOperationException("Test exception for all levels");

        // Debug with exception
        _logger.Debug("Debug level with exception test", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["level"] = "DEBUG"
        });

        // Info with exception
        _logger.Info("Info level test", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["level"] = "INFO"
        });

        // Warn with exception
        _logger.Warn(testException, "Warning level with exception test", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["level"] = "WARNING"
        });

        // Error with exception
        _logger.Error(testException, "Error level with exception test", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["level"] = "ERROR"
        });

        // Critical with exception
        _logger.Critical(testException, "Critical level with exception test", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["level"] = "CRITICAL"
        });

        return Ok(new
        {
            message = "All log levels with exception tested",
            correlationId,
            levelsLogged = new[] { "DEBUG", "INFO", "WARN+Exception", "ERROR+Exception", "CRITICAL+Exception" }
        });
    }

    /// <summary>
    /// Test role-based authorization
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var roles = User.Claims.Where(c => c.Type.Contains("role")).Select(c => c.Value).ToList();
        var matched = HttpContext.GetMatchedIssuer();

        _logger.Info("Admin endpoint accessed", new Dictionary<string, object?>
        {
            ["roles"] = string.Join(",", roles),
            ["claimsCount"] = claims.Count,
            ["provider"] = matched?.Provider ?? matched?.Name ?? "unknown",
            ["issuer"] = matched?.Issuer
        });

        return Ok(new
        {
            message = "✅ Welcome Admin! You have elevated privileges.",
            roles,
            claimsCount = claims.Count,
            provider = matched?.Provider ?? matched?.Name ?? "unknown",
            issuer = matched?.Issuer
        });
    }

    /// <summary>
    /// Test user role authorization
    /// </summary>
    [HttpGet("user-only")]
    [Authorize(Roles = "User,Admin")]
    public IActionResult UserOnly()
    {
        var roles = User.Claims.Where(c => c.Type.Contains("role")).Select(c => c.Value).ToList();
        var matched = HttpContext.GetMatchedIssuer();

        return Ok(new
        {
            message = "✅ Welcome User! You have standard access.",
            roles,
            provider = matched?.Provider ?? matched?.Name ?? "unknown",
            issuer = matched?.Issuer
        });
    }

    /// <summary>
    /// Test timer with long operation
    /// </summary>
    [HttpGet("test-timer/{delayMs:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> TestTimer(int delayMs)
    {
        var correlationId = _logger.GenerateCorrelationId();
        var timer = _logger.StartTimer();

        _logger.Info("Starting timed operation", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["expectedDelayMs"] = delayMs
        });

        await Task.Delay(Math.Min(delayMs, 5000)); // Max 5 seconds

        timer.Done("Timed operation completed", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["requestedDelayMs"] = delayMs
        });

        return Ok(new
        {
            message = "Timer test completed",
            correlationId,
            requestedDelayMs = delayMs,
            note = "Check logs for 'duration' field"
        });
    }

    /// <summary>
    /// Test nested context and complex objects
    /// </summary>
    [HttpPost("test-complex-logging")]
    [AllowAnonymous]
    public IActionResult TestComplexLogging([FromBody] ComplexTestRequest request)
    {
        var correlationId = _logger.GenerateCorrelationId();

        // Log with nested object
        _logger.Info("Complex logging test", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["user"] = new
            {
                id = request.UserId,
                name = request.UserName,
                email = request.UserEmail // Should be masked!
            },
            ["order"] = new
            {
                items = request.Items?.Count ?? 0,
                total = request.Total
            },
            ["metadata"] = new
            {
                timestamp = DateTime.UtcNow,
                version = "1.0"
            }
        });

        return Ok(new
        {
            message = "Complex logging test completed",
            correlationId,
            note = "Check logs for nested objects and PII masking"
        });
    }
}

public class ExceptionTestRequest
{
    public string? ExceptionType { get; set; }
}

public class ComplexTestRequest
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; } // Should be masked in logs
    public List<string>? Items { get; set; }
    public decimal Total { get; set; }
}
