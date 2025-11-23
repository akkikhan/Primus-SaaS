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
export declare class Logger {
    private options;
    private minLevelValue;
    constructor(options: LoggerOptions);
    /**
     * Log a DEBUG message
     */
    debug(message: string, context?: Record<string, any>): void;
    /**
     * Log an INFO message
     */
    info(message: string, context?: Record<string, any>): void;
    /**
     * Log a WARNING message
     */
    warn(message: string, context?: Record<string, any>): void;
    /**
     * Log an ERROR message
     */
    error(message: string, context?: Record<string, any>): void;
    /**
     * Log a CRITICAL message
     */
    critical(message: string, context?: Record<string, any>): void;
    /**
     * Start a performance timer
     */
    startTimer(): Timer;
    /**
     * Generate a correlation ID for distributed tracing
     */
    generateCorrelationId(): string;
    /**
     * Internal log method
     */
    private log;
    /**
     * Get base context (always added)
     */
    private getBaseContext;
    /**
     * Write log entry to all targets
     */
    private writeToTargets;
}
//# sourceMappingURL=Logger.d.ts.map