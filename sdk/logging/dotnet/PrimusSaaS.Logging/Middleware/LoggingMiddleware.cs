using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Middleware;

/// <summary>
/// Middleware to automatically enrich logs with HTTP context
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Logger _logger;
    private readonly Func<Core.LoggingMetricsSnapshot>? _metricsSnapshot;
    private readonly ILogger _loggerAdapter;

    public LoggingMiddleware(RequestDelegate next, Logger logger, Func<Core.LoggingMetricsSnapshot>? metricsSnapshot = null)
    {
        _next = next;
        _logger = logger;
        _metricsSnapshot = metricsSnapshot;
        _loggerAdapter = LoggerFactory.Create(builder => { }).CreateLogger("PrimusMiddleware");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Generate or retrieve Request ID and Correlation ID
        string requestId;
        if (context.Request.Headers.TryGetValue("X-Request-ID", out var headerId))
        {
            requestId = headerId.ToString();
        }
        else
        {
            requestId = $"req-{Guid.NewGuid():N}";
        }

        string correlationId;
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var corrHeader))
        {
            correlationId = corrHeader.ToString();
        }
        else
        {
            correlationId = $"corr-{Guid.NewGuid():N}";
        }

        // 2. Store in context for Logger to use
        context.Items["PrimusRequestId"] = requestId;
        context.Items["PrimusCorrelationId"] = correlationId;

        // 3. Add to Response Headers so client can see it (immediately and on-start to be safe)
        context.Response.Headers["X-Request-ID"] = requestId;
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Request-ID"] = requestId;
            context.Response.Headers["X-Correlation-ID"] = correlationId;
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
                    context.Items["PrimusUser"] = new Dictionary<string, object?>
                    {
                        ["userId"] = userId,
                        ["email"] = email ?? "unknown"
                    };
                }
            }
        }

        var requestScope = new Dictionary<string, object?>
        {
            ["requestId"] = requestId,
            ["correlationId"] = correlationId
        };

        using (_logger.BeginScope(requestScope))
        {
            var start = DateTime.UtcNow;
            try
            {
                // Log request start (Changed to DEBUG to reduce noise)
                _logger.Debug($"{context.Request.Method} {context.Request.Path}", new Dictionary<string, object?>
                {
                    ["method"] = context.Request.Method,
                    ["path"] = context.Request.Path.ToString(),
                    ["queryString"] = context.Request.QueryString.ToString()
                });

                await _next(context);

                // Log request completion (Keep at INFO for access logging)
                var durationMs = (DateTime.UtcNow - start).TotalMilliseconds;
                Generated.LoggingMessages.RequestCompleted(_loggerAdapter, context.Response.StatusCode);

                _logger.Info($"Request completed with status {context.Response.StatusCode}", new Dictionary<string, object?>
                {
                    ["statusCode"] = context.Response.StatusCode,
                    ["method"] = context.Request.Method,
                    ["path"] = context.Request.Path.ToString(),
                    ["durationMs"] = durationMs
                });
            }
            catch (Exception ex)
            {
                var errorContext = new Dictionary<string, object?>
                {
                    ["exception"] = ex.GetType().Name,
                    ["message"] = ex.Message,
                    ["stackTrace"] = ex.StackTrace ?? string.Empty
                };

                var metrics = _metricsSnapshot?.Invoke();
                if (metrics != null)
                {
                    errorContext["loggingDrops"] = metrics.DroppedEntries;
                    errorContext["loggingWriteFailures"] = metrics.WriteFailures;
                    errorContext["loggingAdapterForwarded"] = metrics.AdapterForwardedEntries;
                }

                Generated.LoggingMessages.RequestStart(_loggerAdapter, context.Request.Method, context.Request.Path);
                _logger.Error($"Unhandled exception: {ex.Message}", errorContext);
                throw;
            }
            finally
            {
                _logger.SetHttpContext(null);
            }
        }
    }
}
