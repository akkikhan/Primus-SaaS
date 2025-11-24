using Microsoft.AspNetCore.Http;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Middleware;
using PrimusSaaS.Logging.Targets;
using Xunit;

namespace PrimusSaaS.Logging.Tests;

public class LoggingMiddlewareTests
{
    [Fact]
    public async Task Middleware_ShouldSetCorrelationHeaders_AndScope()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"primus_mw_{Guid.NewGuid():N}.log");
        var fileTarget = new FileTarget(tempFile);
        var logger = new Logger(new LoggerOptions
        {
            ApplicationId = "APP",
            Environment = "dev",
            MinLevel = LogLevel.Debug,
            Metrics = new LoggingMetrics(),
            Targets = new List<TargetConfig>(), // use custom target for direct control
            CustomTargets = new List<ITarget> { fileTarget }
        });

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = 201;
            return Task.CompletedTask;
        };

        var middleware = new LoggingMiddleware(next, logger, logger.GetMetricsSnapshot);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/test";

        await middleware.InvokeAsync(httpContext);

        Assert.True(httpContext.Response.Headers.ContainsKey("X-Request-ID"));
        Assert.True(httpContext.Response.Headers.ContainsKey("X-Correlation-ID"));

        // Close target to release file handle for reading
        fileTarget.Close();

        // Read file logs and find completion entry
        var lines = File.ReadAllLines(tempFile);
        var completion = lines.Last(line => line.Contains("Request completed with status 201"));

        using var doc = System.Text.Json.JsonDocument.Parse(completion);
        var context = doc.RootElement.GetProperty("context");
        Assert.True(context.TryGetProperty("correlationId", out _));
        Assert.True(context.TryGetProperty("requestId", out _));

        try { File.Delete(tempFile); } catch { }
    }
}
