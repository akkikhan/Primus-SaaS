using Microsoft.AspNetCore.Http;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Middleware;

/// <summary>
/// Middleware to automatically enrich logs with HTTP context
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Logger _logger;

    public LoggingMiddleware(RequestDelegate next, Logger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Set HTTP context for enrichment
        _logger.SetHttpContext(context);

        // Simulate Primus Identity Validator context (in real app, this would be set by the validator)
        // This is just for demonstration - the actual Identity Validator would set these
        if (!context.Items.ContainsKey("PrimusUser"))
        {
            // Example: Extract from claims if using standard ASP.NET Identity
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("sub")?.Value 
                          ?? context.User.FindFirst("userId")?.Value
                          ?? context.User.Identity.Name;
                
                var email = context.User.FindFirst("email")?.Value;

                if (userId != null)
                {
                    context.Items["PrimusUser"] = new Dictionary<string, object>
                    {
                        ["userId"] = userId,
                        ["email"] = email ?? "unknown"
                    };
                }
            }
        }

        try
        {
            // Log request start
            _logger.Info($"{context.Request.Method} {context.Request.Path}", new Dictionary<string, object>
            {
                ["method"] = context.Request.Method,
                ["path"] = context.Request.Path.ToString(),
                ["queryString"] = context.Request.QueryString.ToString()
            });

            await _next(context);

            // Log request completion
            _logger.Info($"Request completed with status {context.Response.StatusCode}", new Dictionary<string, object>
            {
                ["statusCode"] = context.Response.StatusCode,
                ["method"] = context.Request.Method,
                ["path"] = context.Request.Path.ToString()
            });
        }
        catch (Exception ex)
        {
            // Log unhandled exceptions
            _logger.Error($"Unhandled exception: {ex.Message}", new Dictionary<string, object>
            {
                ["exception"] = ex.GetType().Name,
                ["message"] = ex.Message,
                ["stackTrace"] = ex.StackTrace ?? string.Empty
            });
            throw;
        }
        finally
        {
            // Clear HTTP context
            _logger.SetHttpContext(null);
        }
    }
}
