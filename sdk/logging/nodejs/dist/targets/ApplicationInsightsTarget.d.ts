import { LogEntry } from '../core/LogEntry';
import { Target } from './Target';
export interface ApplicationInsightsTargetOptions {
    connectionString: string;
    roleName?: string;
}
/**
 * Sends structured logs to Azure Application Insights.
 */
export declare class ApplicationInsightsTarget implements Target {
    private client;
    constructor(options: ApplicationInsightsTargetOptions);
    write(logEntry: LogEntry): void;
}
//# sourceMappingURL=ApplicationInsightsTarget.d.ts.map