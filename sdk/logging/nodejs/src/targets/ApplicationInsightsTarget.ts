import appInsights, { Contracts, TelemetryClient } from 'applicationinsights';
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
    if (!appInsights.defaultClient) {
      appInsights
        .setup(options.connectionString)
        .setAutoCollectRequests(false)
        .setAutoCollectPerformance(false)
        .setAutoCollectExceptions(false)
        .setAutoCollectDependencies(false)
        .setAutoDependencyCorrelation(false)
        .setAutoCollectConsole(false)
        .setUseDiskRetryCaching(false)
        .start();
    }

    this.client = appInsights.defaultClient;
    this.client.config.connectionString = options.connectionString;

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

function mapSeverity(level: LogLevel): Contracts.SeverityLevel {
  switch (level) {
    case LogLevel.DEBUG:
      return Contracts.SeverityLevel.Verbose;
    case LogLevel.INFO:
      return Contracts.SeverityLevel.Information;
    case LogLevel.WARNING:
      return Contracts.SeverityLevel.Warning;
    case LogLevel.ERROR:
      return Contracts.SeverityLevel.Error;
    case LogLevel.CRITICAL:
      return Contracts.SeverityLevel.Critical;
    default:
      return Contracts.SeverityLevel.Information;
  }
}
