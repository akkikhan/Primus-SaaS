import { LogLevel, LogLevelValue } from './LogLevel';
import { LogEntry, createLogEntry } from './LogEntry';
import { LoggerOptions } from './LoggerOptions';

/**
 * Timer for performance tracking
 */
export interface Timer {
    done(message: string, context?: Record<string, any>): void;
}

/**
 * Main Logger class
 */
export class Logger {
    private options: LoggerOptions;
    private minLevelValue: number;

    constructor(options: LoggerOptions) {
        this.options = {
            minLevel: LogLevel.INFO,
            targets: [{ type: 'console' }],
            ...options
        };

        this.minLevelValue = LogLevelValue[this.options.minLevel!];
    }

    /**
     * Log a DEBUG message
     */
    debug(message: string, context: Record<string, any> = {}): void {
        this.log(LogLevel.DEBUG, message, context);
    }

    /**
     * Log an INFO message
     */
    info(message: string, context: Record<string, any> = {}): void {
        this.log(LogLevel.INFO, message, context);
    }

    /**
     * Log a WARNING message
     */
    warn(message: string, context: Record<string, any> = {}): void {
        this.log(LogLevel.WARNING, message, context);
    }

    /**
     * Log an ERROR message
     */
    error(message: string, context: Record<string, any> = {}): void {
        this.log(LogLevel.ERROR, message, context);
    }

    /**
     * Log a CRITICAL message
     */
    critical(message: string, context: Record<string, any> = {}): void {
        this.log(LogLevel.CRITICAL, message, context);
    }

    /**
     * Start a performance timer
     */
    startTimer(): Timer {
        const startTime = Date.now();

        return {
            done: (message: string, context: Record<string, any> = {}) => {
                const duration = Date.now() - startTime;
                this.info(message, { ...context, duration });
            }
        };
    }

    /**
     * Generate a correlation ID for distributed tracing
     */
    generateCorrelationId(): string {
        return `corr-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Internal log method
     */
    private log(level: LogLevel, message: string, context: Record<string, any>): void {
        // Filter by log level
        if (LogLevelValue[level] < this.minLevelValue) {
            return;
        }

        // Create log entry
        const logEntry = createLogEntry(level, message, {
            ...this.getBaseContext(),
            ...context
        });

        // Write to targets (for now, just console)
        this.writeToTargets(logEntry);
    }

    /**
     * Get base context (always added)
     */
    private getBaseContext(): Record<string, any> {
        return {
            applicationId: this.options.applicationId,
            environment: this.options.environment
        };
    }

    /**
     * Write log entry to all targets
     */
    private writeToTargets(logEntry: LogEntry): void {
        // For now, just write to console
        // We'll implement proper targets in Day 4
        console.log(JSON.stringify(logEntry));
    }
}
