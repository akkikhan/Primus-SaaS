"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Logger = void 0;
const LogLevel_1 = require("./LogLevel");
const LogEntry_1 = require("./LogEntry");
const Context_1 = require("./Context");
const ConsoleTarget_1 = require("../targets/ConsoleTarget");
const FileTarget_1 = require("../targets/FileTarget");
const ApplicationInsightsTarget_1 = require("../targets/ApplicationInsightsTarget");
const Masker_1 = require("../masking/Masker");
/**
 * Main Logger class
 */
class Logger {
    constructor(options) {
        this.targets = [];
        this.buffer = [];
        this.flushing = false;
        this.options = {
            minLevel: LogLevel_1.LogLevel.INFO,
            targets: [{ type: 'console' }],
            ...options
        };
        this.minLevelValue = LogLevel_1.LogLevelValue[this.options.minLevel];
        this.contextManager = new Context_1.Context(this.options.enrichers ?? []);
        const maskingConfig = (0, Masker_1.resolveMaskingConfig)(this.options.masking);
        this.masker = new Masker_1.Masker(maskingConfig);
        this.buffering = resolveBufferingConfig(this.options.buffering);
        this.initializeTargets();
        if (this.buffering.enabled && this.buffering.flushOnExit) {
            this.registerFlushOnExit();
        }
    }
    initializeTargets() {
        if (!this.options.targets || this.options.targets.length === 0) {
            this.targets.push(new ConsoleTarget_1.ConsoleTarget());
            return;
        }
        for (const config of this.options.targets) {
            const target = this.createTarget(config);
            if (target) {
                this.targets.push(target);
            }
        }
    }
    createTarget(config) {
        switch (config.type) {
            case 'console':
                return new ConsoleTarget_1.ConsoleTarget({ pretty: config.pretty });
            case 'file':
                if (!config.path) {
                    console.warn('File target requires a path. Falling back to console.');
                    return new ConsoleTarget_1.ConsoleTarget({ pretty: config.pretty });
                }
                return new FileTarget_1.FileTarget({
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
                return new ApplicationInsightsTarget_1.ApplicationInsightsTarget({
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
    setRequest(request) {
        this.currentRequest = request;
    }
    /**
     * Clear the current request
     */
    clearRequest() {
        this.currentRequest = undefined;
    }
    /**
     * Log a DEBUG message
     */
    debug(message, context = {}) {
        this.log(LogLevel_1.LogLevel.DEBUG, message, context);
    }
    /**
     * Log an INFO message
     */
    info(message, context = {}) {
        this.log(LogLevel_1.LogLevel.INFO, message, context);
    }
    /**
     * Log a WARNING message
     */
    warn(message, context = {}) {
        this.log(LogLevel_1.LogLevel.WARNING, message, context);
    }
    /**
     * Log an ERROR message
     */
    error(message, context = {}) {
        this.log(LogLevel_1.LogLevel.ERROR, message, context);
    }
    /**
     * Log a CRITICAL message
     */
    critical(message, context = {}) {
        this.log(LogLevel_1.LogLevel.CRITICAL, message, context);
    }
    /**
     * Start a performance timer
     */
    startTimer() {
        const startTime = Date.now();
        return {
            done: (message, context = {}) => {
                const duration = Date.now() - startTime;
                this.info(message, { ...context, duration });
            }
        };
    }
    /**
     * Generate a correlation ID for distributed tracing
     */
    generateCorrelationId() {
        return `corr-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }
    /**
     * Internal log method
     */
    log(level, message, context) {
        // Filter by log level
        if (LogLevel_1.LogLevelValue[level] < this.minLevelValue) {
            return;
        }
        // Get base context
        const baseContext = this.getBaseContext();
        // Enrich with request context (if available)
        const enrichedContext = this.contextManager.enrich({ ...baseContext, ...context }, this.currentRequest);
        // Create log entry
        const maskedContext = this.masker ? this.masker.mask(enrichedContext) : enrichedContext;
        const logEntry = (0, LogEntry_1.createLogEntry)(level, message, maskedContext);
        if (this.buffering.enabled) {
            this.buffer.push(logEntry);
            if (this.buffer.length >= this.buffering.bufferSize) {
                void this.flushBuffer();
            }
            else {
                this.scheduleFlush();
            }
        }
        else {
            void this.writeToTargets(logEntry);
        }
    }
    /**
     * Get base context (always added)
     */
    getBaseContext() {
        return {
            applicationId: this.options.applicationId,
            environment: this.options.environment
        };
    }
    /**
     * Write log entry to all targets
     */
    async writeToTargets(logEntry) {
        for (const target of this.targets) {
            try {
                const result = target.write(logEntry);
                if (result instanceof Promise) {
                    await result;
                }
            }
            catch (error) {
                console.error('Failed to write to log target:', error);
            }
        }
    }
    scheduleFlush() {
        if (this.flushTimer || !this.buffering.enabled) {
            return;
        }
        this.flushTimer = setTimeout(() => {
            this.flushTimer = undefined;
            void this.flushBuffer();
        }, this.buffering.flushIntervalMs);
    }
    async flushBuffer() {
        if (!this.buffering.enabled || this.buffer.length === 0 || this.flushing) {
            return;
        }
        this.flushing = true;
        const entries = this.buffer.splice(0, this.buffer.length);
        try {
            for (const entry of entries) {
                await this.writeToTargets(entry);
            }
        }
        finally {
            this.flushing = false;
        }
    }
    registerFlushOnExit() {
        const handler = async () => {
            await this.flushBuffer();
        };
        process.on('beforeExit', handler);
        process.on('SIGINT', handler);
        process.on('SIGTERM', handler);
    }
}
exports.Logger = Logger;
function resolveBufferingConfig(config) {
    const resolved = {
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
//# sourceMappingURL=Logger.js.map