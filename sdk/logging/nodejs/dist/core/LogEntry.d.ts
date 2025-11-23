import { LogLevel } from './LogLevel';
/**
 * Represents a single log entry
 */
export interface LogEntry {
    /** ISO 8601 timestamp */
    timestamp: string;
    /** Log level */
    level: LogLevel;
    /** Log message */
    message: string;
    /** Contextual data */
    context: Record<string, any>;
}
/**
 * Creates a log entry
 */
export declare function createLogEntry(level: LogLevel, message: string, context?: Record<string, any>): LogEntry;
//# sourceMappingURL=LogEntry.d.ts.map