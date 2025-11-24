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
        // 1. Generate or retrieve Request ID
        string requestId;
        if (context.Request.Headers.TryGetValue("X-Request-ID", out var headerId))
        {
            requestId = headerId.ToString();
        }
        else
        {
            requestId = $"req-{Guid.NewGuid():N}";
        }

        // 2. Store in context for Logger to use
        context.Items["PrimusRequestId"] = requestId;

        // 3. Add to Response Headers so client can see it
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Request-ID"] = requestId;
            return Task.CompletedTask;
        });

        // Set HTTP context for enrichment
        _logger.SetHttpContext(context);

        // Extract user context from standard ASP.NET Identity claims
        // Works with any authentication system (JWT, Cookie, OAuth, etc.)
        if (!context.Items.ContainsKey("PrimusUser"))
        {
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
            // Log request start (Changed to DEBUG to reduce noise)
            _logger.Debug($"{context.Request.Method} {context.Request.Path}", new Dictionary<string, object>
            {
                ["method"] = context.Request.Method,
                ["path"] = context.Request.Path.ToString(),
                ["queryString"] = context.Request.QueryString.ToString()
            });

            await _next(context);

            // Log request completion (Keep at INFO for access logging)
            _logger.Info($"Request completed with status {context.Response.StatusCode}", new Dictionary<string, object>
            {
                ["statusCode"] = context.Response.StatusCode,
                ["method"] = context.Request.Method,
                ["path"] = context.Request.Path.ToString(),
                ["duration"] = 0 // We should ideally track duration here too
            });
        }
        catch (Exception ex)
        {
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
            _logger.SetHttpContext(null);
        }
    }
}
