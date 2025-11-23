"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Logger = void 0;
const LogLevel_1 = require("./LogLevel");
const LogEntry_1 = require("./LogEntry");
const Context_1 = require("./Context");
const ConsoleTarget_1 = require("../targets/ConsoleTarget");
const FileTarget_1 = require("../targets/FileTarget");
/**
 * Main Logger class
 */
class Logger {
    constructor(options) {
        this.targets = [];
        this.options = {
            minLevel: LogLevel_1.LogLevel.INFO,
            targets: [{ type: 'console' }],
            ...options
        };
        this.minLevelValue = LogLevel_1.LogLevelValue[this.options.minLevel];
        this.contextManager = new Context_1.Context();
        this.initializeTargets();
    }
    initializeTargets() {
        if (!this.options.targets)
            return;
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
                return new FileTarget_1.FileTarget({ path: config.path });
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
        const logEntry = (0, LogEntry_1.createLogEntry)(level, message, enrichedContext);
        // Write to all targets
        this.writeToTargets(logEntry);
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
    writeToTargets(logEntry) {
        for (const target of this.targets) {
            try {
                target.write(logEntry);
            }
            catch (error) {
                console.error('Failed to write to log target:', error);
            }
        }
    }
}
exports.Logger = Logger;
//# sourceMappingURL=Logger.js.map