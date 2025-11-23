"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Logger = void 0;
const LogLevel_1 = require("./LogLevel");
const LogEntry_1 = require("./LogEntry");
/**
 * Main Logger class
 */
class Logger {
    constructor(options) {
        this.options = {
            minLevel: LogLevel_1.LogLevel.INFO,
            targets: [{ type: 'console' }],
            ...options
        };
        this.minLevelValue = LogLevel_1.LogLevelValue[this.options.minLevel];
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
        // Create log entry
        const logEntry = (0, LogEntry_1.createLogEntry)(level, message, {
            ...this.getBaseContext(),
            ...context
        });
        // Write to targets (for now, just console)
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
        // For now, just write to console
        // We'll implement proper targets in Day 4
        console.log(JSON.stringify(logEntry));
    }
}
exports.Logger = Logger;
//# sourceMappingURL=Logger.js.map