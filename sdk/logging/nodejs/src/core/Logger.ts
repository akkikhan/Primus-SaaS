import { LogLevel, LogLevelValue } from './LogLevel';
import { LogEntry, createLogEntry } from './LogEntry';
import { LoggerOptions, TargetConfig } from './LoggerOptions';
import { Context } from './Context';
import { Target } from '../targets/Target';
import { ConsoleTarget } from '../targets/ConsoleTarget';
import { FileTarget } from '../targets/FileTarget';

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
    private contextManager: Context;
    private currentRequest?: any;
    private targets: Target[] = [];

    constructor(options: LoggerOptions) {
        this.options = {
            minLevel: LogLevel.INFO,
            targets: [{ type: 'console' }],
            ...options
        };

        this.minLevelValue = LogLevelValue[this.options.minLevel!];
        this.contextManager = new Context();

        this.initializeTargets();
    }

    private initializeTargets(): void {
        if (!this.options.targets) return;

        for (const config of this.options.targets) {
            const target = this.createTarget(config);
            if (target) {
                this.targets.push(target);
            }
        }
    }

    private createTarget(config: TargetConfig): Target | null {
        switch (config.type) {
            case 'console':
                return new ConsoleTarget({ pretty: config.pretty });
            case 'file':
                return new FileTarget({ path: config.path });
            default:
                console.warn(`Unknown target type: ${config.type}`);
                return null;
        }
    }

    /**
     * Set the current request for context enrichment
     * This should be called by middleware in web frameworks
     */
    setRequest(request: any): void {
        this.currentRequest = request;
    }

    /**
     * Clear the current request
     */
    clearRequest(): void {
        this.currentRequest = undefined;
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

        // Get base context
        const baseContext = this.getBaseContext();

        // Enrich with request context (if available)
        const enrichedContext = this.contextManager.enrich(
            { ...baseContext, ...context },
            this.currentRequest
        );

        // Create log entry
        const logEntry = createLogEntry(level, message, enrichedContext);

        // Write to all targets
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
        for (const target of this.targets) {
            try {
                target.write(logEntry);
            } catch (error) {
                console.error('Failed to write to log target:', error);
            }
        }
    }
}
