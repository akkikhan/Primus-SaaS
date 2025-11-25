using PrimusSaaS.Logging.Targets;
using Microsoft.AspNetCore.Http;

namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Main logger class
/// </summary>
public class Logger
{
    private readonly LoggerOptions _options;
    private readonly List<ITarget> _targets = new();
    private readonly PiiMasker _piiMasker;
    private readonly SafeObjectSerializer _serializer;
    private readonly SafeLogFormatter _formatter;
    private readonly LoggingMetrics _metrics;
    private readonly Health.LoggingHealthReporter _healthReporter;
    private static readonly AsyncLocal<Stack<Dictionary<string, object?>>?> ScopeStack = new();
    private HttpContext? _currentHttpContext;

    public Logger(LoggerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        LoggerOptionsValidator.Validate(_options);

        _options.Serialization ??= new SerializationOptions();

        _piiMasker = new PiiMasker(options.Pii);
        _serializer = new SafeObjectSerializer(_options.Serialization);
        _formatter = new SafeLogFormatter(_serializer);
        _metrics = _options.Metrics ?? new LoggingMetrics();
        InitializeTargets();
        _healthReporter = new Health.LoggingHealthReporter(_metrics, _targets);
    }

    internal LoggingMetrics Metrics => _metrics;

    internal bool IsEnabled(LogLevel level) => level >= _options.MinLevel;

    private void InitializeTargets()
    {
        if (_options.CustomTargets is { Count: > 0 })
        {
            _targets.AddRange(_options.CustomTargets);
            return;
        }

        foreach (var config in _options.Targets)
        {
            var target = CreateTarget(config);
            if (target != null)
            {
                _targets.Add(target);
            }
        }
    }

    private ITarget? CreateTarget(TargetConfig config)
    {
        ITarget? target = config.Type.ToLowerInvariant() switch
        {
            "console" => new ConsoleTarget(config.Pretty),
            "file" => new FileTarget(
                config.Path ?? "logs/app.log", 
                config.MaxFileSize, 
                config.MaxRetainedFiles, 
                config.CompressRotatedFiles
            ),
            "applicationinsights" => new ApplicationInsightsTarget(config.ConnectionString),
            _ => null
        };

        if (target != null && config.Async)
        {
            return new AsyncTargetWrapper(target, config.BufferSize, _metrics);
        }

        return target;
    }

    /// <summary>
    /// Set the current HTTP context for enrichment
    /// </summary>
    public void SetHttpContext(HttpContext? context)
    {
        _currentHttpContext = context;
    }

    /// <summary>
    /// Log a DEBUG message
    /// </summary>
    public void Debug(string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Debug, message, context);
    }

    /// <summary>
    /// Log a DEBUG message with exception
    /// </summary>
    public void Debug(Exception ex, string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Debug, message, context, ex);
    }

    /// <summary>
    /// Log an INFO message
    /// </summary>
    public void Info(string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Info, message, context);
    }

    /// <summary>
    /// Log an INFO message with exception
    /// </summary>
    public void Info(Exception ex, string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Info, message, context, ex);
    }

    /// <summary>
    /// Log a WARNING message
    /// </summary>
    public void Warn(string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Warning, message, context);
    }

    /// <summary>
    /// Log a WARNING message with exception
    /// </summary>
    public void Warn(Exception ex, string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Warning, message, context, ex);
    }

    /// <summary>
    /// Log an ERROR message
    /// </summary>
    public void Error(string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Error, message, context);
    }

    /// <summary>
    /// Log an ERROR message with exception
    /// </summary>
    public void Error(Exception ex, string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Error, message, context, ex);
    }

    /// <summary>
    /// Log a CRITICAL message
    /// </summary>
    public void Critical(string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Critical, message, context);
    }

    /// <summary>
    /// Log a CRITICAL message with exception
    /// </summary>
    public void Critical(Exception ex, string message, Dictionary<string, object?>? context = null)
    {
        Log(LogLevel.Critical, message, context, ex);
    }

    /// <summary>
    /// Start a performance timer
    /// </summary>
    public ITimer StartTimer()
    {
        return new Timer(this);
    }

    /// <summary>
    /// Generate a correlation ID
    /// </summary>
    public string GenerateCorrelationId()
    {
        return $"corr-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Begin a structured scope for ambient context propagation.
    /// </summary>
    public IDisposable BeginScope(Dictionary<string, object?>? state)
    {
        var stack = ScopeStack.Value ??= new Stack<Dictionary<string, object?>>();
        stack.Push(state ?? new Dictionary<string, object?>());

        return new ScopeDisposable(() =>
        {
            if (ScopeStack.Value is { Count: > 0 })
            {
                ScopeStack.Value!.Pop();
                if (ScopeStack.Value!.Count == 0)
                {
                    ScopeStack.Value = null;
                }
            }
        });
    }

    /// <summary>
    /// Exposes current metrics snapshot for health/observability.
    /// </summary>
    public LoggingMetricsSnapshot GetMetricsSnapshot() => _metrics.Snapshot();

    /// <summary>
    /// Exposes a simple health snapshot (metrics + target statuses).
    /// </summary>
    public Health.LoggingHealthSnapshot GetHealthSnapshot() => _healthReporter.Snapshot();

    private void Log(LogLevel level, string message, Dictionary<string, object?>? context, Exception? exception = null)
    {
        // Filter by log level
        if (level < _options.MinLevel)
        {
            return;
        }

        // Get base context
        var enrichedContext = GetBaseContext();

        // Merge scope context (outermost first)
        var scopeStack = ScopeStack.Value;
        if (scopeStack != null && scopeStack.Count > 0)
        {
            foreach (var scope in scopeStack.Reverse())
            {
                foreach (var kvp in scope)
                {
                    enrichedContext[kvp.Key] = kvp.Value;
                }
            }
        }

        // Merge with provided context
        if (context != null)
        {
            foreach (var kvp in context)
            {
                enrichedContext[kvp.Key] = kvp.Value;
            }
        }

        // Add exception details if present
        if (exception != null)
        {
            enrichedContext["exception"] = DeconstructException(exception);
        }

        // Enrich with HTTP context if available
        if (_currentHttpContext != null)
        {
            EnrichWithHttpContext(enrichedContext);
        }

        // Apply custom enrichers
        foreach (var enricher in _options.Enrichers)
        {
            try
            {
                enricher.Enrich(enrichedContext);
            }
            catch (Exception ex)
            {
                // Don't let enrichers crash logging
                Console.Error.WriteLine($"Enricher failed: {ex.Message}");
            }
        }

        // Mask PII
        var maskedMessage = _piiMasker.MaskMessage(message);
        var maskedContext = _piiMasker.MaskContext(enrichedContext);

        // Sanitize for safe serialization
        var safeContext = _serializer.SanitizeContext(maskedContext);

        // Create log entry
        var logEntry = LogEntry.Create(level, maskedMessage, safeContext, _formatter);

        // Write to all targets
        WriteToTargets(logEntry);
    }

    private Dictionary<string, object?> DeconstructException(Exception ex)
    {
        var dict = new Dictionary<string, object?>
        {
            ["type"] = ex.GetType().Name,
            ["message"] = _piiMasker.MaskMessage(ex.Message), // Mask PII in exception message
            ["stackTrace"] = ex.StackTrace ?? string.Empty
        };

        if (ex.InnerException != null)
        {
            dict["inner"] = DeconstructException(ex.InnerException);
        }

        return dict;
    }

    private Dictionary<string, object?> GetBaseContext()
    {
        return new Dictionary<string, object?>
        {
            ["applicationId"] = _options.ApplicationId,
            ["environment"] = _options.Environment
        };
    }

    private void EnrichWithHttpContext(Dictionary<string, object?> context)
    {
        if (_currentHttpContext == null) return;

        try
        {
            // Add request ID
            if (_currentHttpContext.Items.TryGetValue("PrimusRequestId", out var requestId))
            {
                context["requestId"] = requestId;
            }
            else if (_currentHttpContext.Request.Headers.TryGetValue("X-Request-ID", out var headerId))
            {
                context["requestId"] = headerId.ToString();
            }
            else
            {
                context["requestId"] = $"req-{Guid.NewGuid():N}";
            }

            // Add user context (from Identity Validator or ASP.NET Identity)
            if (_currentHttpContext.Items.TryGetValue("PrimusUser", out var primusUser))
            {
                var userDict = primusUser as Dictionary<string, object?>;
                if (userDict != null)
                {
                    if (userDict.TryGetValue("userId", out var userId))
                        context["userId"] = userId;
                    if (userDict.TryGetValue("email", out var email))
                        context["userEmail"] = email;
                }
            }

            // Add tenant context
            if (_currentHttpContext.Items.TryGetValue("PrimusTenantContext", out var tenantContext))
            {
                var tenantDict = tenantContext as Dictionary<string, object?>;
                if (tenantDict != null)
                {
                    if (tenantDict.TryGetValue("tenantId", out var tenantId))
                        context["tenantId"] = tenantId;
                }
            }
        }
        catch (Exception ex)
        {
            context["httpContextError"] = ex.Message;
        }
    }

    private void WriteToTargets(LogEntry logEntry)
    {
        foreach (var target in _targets)
        {
            try
            {
                target.Write(logEntry);
                _metrics.IncrementWritten();
            }
            catch (Exception ex)
            {
                _metrics.IncrementFailure();
                Console.Error.WriteLine($"Failed to write to log target: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// Timer interface for performance tracking
/// </summary>
public interface ITimer
{
    void Done(string message, Dictionary<string, object?>? context = null);
}

/// <summary>
/// Timer implementation
/// </summary>
internal class Timer : ITimer
{
    private readonly Logger _logger;
    private readonly DateTime _startTime;

    public Timer(Logger logger)
    {
        _logger = logger;
        _startTime = DateTime.UtcNow;
    }

    public void Done(string message, Dictionary<string, object?>? context = null)
    {
        var duration = (DateTime.UtcNow - _startTime).TotalMilliseconds;
        var enrichedContext = context ?? new Dictionary<string, object?>();
        enrichedContext["duration"] = duration;
        _logger.Info(message, enrichedContext);
    }
}

internal sealed class ScopeDisposable : IDisposable
{
    private readonly Action _onDispose;
    private bool _disposed;

    public ScopeDisposable(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _onDispose();
    }
}
