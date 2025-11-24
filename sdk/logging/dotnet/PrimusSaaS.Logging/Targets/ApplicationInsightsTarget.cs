using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using PrimusSaaS.Logging.Core;

namespace PrimusSaaS.Logging.Targets;

/// <summary>
/// Target for sending logs to Azure Application Insights
/// </summary>
public class ApplicationInsightsTarget : ITarget
{
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsTarget(string? connectionString)
    {
        var config = TelemetryConfiguration.CreateDefault();
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            config.ConnectionString = connectionString;
        }

        _telemetryClient = new TelemetryClient(config);
    }

    public void Write(LogEntry logEntry)
    {
        var severityLevel = MapSeverityLevel(logEntry.Level);
        var telemetry = new TraceTelemetry(logEntry.Message, severityLevel);

        // Add timestamp
        telemetry.Timestamp = logEntry.Timestamp;

        // Add context properties
        foreach (var kvp in logEntry.Context)
        {
            if (kvp.Value != null)
            {
                telemetry.Properties[kvp.Key] = kvp.Value.ToString();
            }
        }

        // Track exception if present in context
        if (logEntry.Context.TryGetValue("exception", out var exceptionObj) && exceptionObj is Exception ex)
        {
            var exceptionTelemetry = new ExceptionTelemetry(ex)
            {
                SeverityLevel = severityLevel,
                Timestamp = logEntry.Timestamp
            };

            // Copy properties to exception telemetry too
            foreach (var kvp in logEntry.Context)
            {
                if (kvp.Key != "exception" && kvp.Value != null)
                {
                    exceptionTelemetry.Properties[kvp.Key] = kvp.Value.ToString();
                }
            }

            _telemetryClient.TrackException(exceptionTelemetry);
        }
        else
        {
            _telemetryClient.TrackTrace(telemetry);
        }
    }

    public void Close()
    {
        _telemetryClient.Flush();
        // Allow some time for flushing
        Thread.Sleep(1000);
    }

    private SeverityLevel MapSeverityLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => SeverityLevel.Verbose,
            LogLevel.Info => SeverityLevel.Information,
            LogLevel.Warning => SeverityLevel.Warning,
            LogLevel.Error => SeverityLevel.Error,
            LogLevel.Critical => SeverityLevel.Critical,
            _ => SeverityLevel.Information
        };
    }
}
