import { LogLevel, LogLevelValue } from './LogLevel';
import { LogEntry, createLogEntry } from './LogEntry';
import { BufferingConfig, LoggerOptions, MaskingConfig, TargetConfig } from './LoggerOptions';
import { Context } from './Context';
import { Target } from '../targets/Target';
import { ConsoleTarget } from '../targets/ConsoleTarget';
import { FileTarget } from '../targets/FileTarget';
import { ApplicationInsightsTarget } from '../targets/ApplicationInsightsTarget';
import { Masker, resolveMaskingConfig } from '../masking/Masker';

interface ResolvedBufferingConfig {
    enabled: boolean;
    bufferSize: number;
    flushIntervalMs: number;
    flushOnExit: boolean;
}

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
    private masker: Masker;
    private buffering: ResolvedBufferingConfig;
    private buffer: LogEntry[] = [];
    private flushTimer?: NodeJS.Timeout;
    private flushing = false;

    constructor(options: LoggerOptions) {
        this.options = {
            minLevel: LogLevel.INFO,
            targets: [{ type: 'console' }],
            ...options
        };

        this.minLevelValue = LogLevelValue[this.options.minLevel!];
        this.contextManager = new Context(this.options.enrichers ?? []);
        const maskingConfig = resolveMaskingConfig(this.options.masking as MaskingConfig | undefined);
        this.masker = new Masker(maskingConfig);
        this.buffering = resolveBufferingConfig(this.options.buffering);

        this.initializeTargets();

        if (this.buffering.enabled && this.buffering.flushOnExit) {
            this.registerFlushOnExit();
        }
    }

    private initializeTargets(): void {
        if (!this.options.targets || this.options.targets.length === 0) {
            this.targets.push(new ConsoleTarget());
            return;
        }

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
                if (!config.path) {
                    console.warn('File target requires a path. Falling back to console.');
                    return new ConsoleTarget({ pretty: config.pretty });
                }
                return new FileTarget({
                    path: config.path,
                    maxFileSize: config.maxFileSize,
                    maxRetainedFiles: config.maxRetainedFiles,
                    compressRotatedFiles: config.compressRotatedFiles
                });
            case 'application-insights':
            case 'applicationInsights':
                if (!config.connectionString) {
                    console.warn('Application Insights target requires a connectionString. Skipping target.');
                    return null;
                }
                return new ApplicationInsightsTarget({
                    connectionString: config.connectionString,
                    roleName: config.roleName
                });
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
        const maskedContext = this.masker ? this.masker.mask(enrichedContext) : enrichedContext;
        const logEntry = createLogEntry(level, message, maskedContext);

        if (this.buffering.enabled) {
            this.buffer.push(logEntry);
            if (this.buffer.length >= this.buffering.bufferSize) {
                void this.flushBuffer();
            } else {
                this.scheduleFlush();
            }
        } else {
            void this.writeToTargets(logEntry);
        }
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
    private async writeToTargets(logEntry: LogEntry): Promise<void> {
        for (const target of this.targets) {
            try {
                const result = target.write(logEntry);
                if (result instanceof Promise) {
                    await result;
                }
            } catch (error) {
                console.error('Failed to write to log target:', error);
            }
        }
    }

    private scheduleFlush(): void {
        if (this.flushTimer || !this.buffering.enabled) {
            return;
        }

        this.flushTimer = setTimeout(() => {
            this.flushTimer = undefined;
            void this.flushBuffer();
        }, this.buffering.flushIntervalMs);
    }

    private async flushBuffer(): Promise<void> {
        if (!this.buffering.enabled || this.buffer.length === 0 || this.flushing) {
            return;
        }

        this.flushing = true;
        const entries = this.buffer.splice(0, this.buffer.length);

        try {
            for (const entry of entries) {
                await this.writeToTargets(entry);
            }
        } finally {
            this.flushing = false;
        }
    }

    private registerFlushOnExit(): void {
        const handler = async () => {
            await this.flushBuffer();
        };

        process.on('beforeExit', handler);
        process.on('SIGINT', handler);
        process.on('SIGTERM', handler);
    }
}

function resolveBufferingConfig(config?: BufferingConfig): ResolvedBufferingConfig {
    const resolved: ResolvedBufferingConfig = {
        enabled: config?.enabled ?? false,
        bufferSize: config?.bufferSize ?? 100,
        flushIntervalMs: config?.flushIntervalMs ?? config?.flushInterval ?? 2000,
        flushOnExit: config?.flushOnExit ?? true
    };

    if (resolved.bufferSize <= 0) {
        resolved.bufferSize = 1;
    }

    if (resolved.flushIntervalMs <= 0) {
        resolved.flushIntervalMs = 2000;
    }

    return resolved;
}
