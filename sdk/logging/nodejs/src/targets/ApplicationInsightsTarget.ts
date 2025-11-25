import appInsights, { KnownSeverityLevel, TelemetryClient } from 'applicationinsights';
import { LogEntry } from '../core/LogEntry';
import { LogLevel } from '../core/LogLevel';
import { Target } from './Target';

export interface ApplicationInsightsTargetOptions {
  connectionString: string;
  roleName?: string;
}

/**
 * Sends structured logs to Azure Application Insights.
 */
export class ApplicationInsightsTarget implements Target {
  private client: TelemetryClient;

  constructor(options: ApplicationInsightsTargetOptions) {
    const client = appInsights.defaultClient ?? new TelemetryClient(options.connectionString);
    if (!appInsights.defaultClient) {
      appInsights
        .setup(options.connectionString)
        .setAutoCollectRequests(false)
        .setAutoCollectPerformance(false, false)
        .setAutoCollectExceptions(false)
        .setAutoCollectDependencies(false)
        .setAutoDependencyCorrelation(false)
        .setAutoCollectConsole(false)
        .setUseDiskRetryCaching(false)
        .start();
    }

    client.config.connectionString = options.connectionString;
    this.client = client;

    if (options.roleName) {
      this.client.context.tags[this.client.context.keys.cloudRole] = options.roleName;
    }
  }

  write(logEntry: LogEntry): void {
    try {
      this.client.trackTrace({
        message: logEntry.message,
        severity: mapSeverity(logEntry.level),
        properties: {
          ...logEntry.context,
          level: logEntry.level,
          timestamp: logEntry.timestamp
        }
      });
    } catch (err) {
      console.error('ApplicationInsightsTarget failed to send log', err);
    }
  }
}

function mapSeverity(level: LogLevel): KnownSeverityLevel {
  switch (level) {
    case LogLevel.DEBUG:
      return KnownSeverityLevel.Verbose;
    case LogLevel.INFO:
      return KnownSeverityLevel.Information;
    case LogLevel.WARNING:
      return KnownSeverityLevel.Warning;
    case LogLevel.ERROR:
      return KnownSeverityLevel.Error;
    case LogLevel.CRITICAL:
      return KnownSeverityLevel.Critical;
    default:
      return KnownSeverityLevel.Information;
  }
}
