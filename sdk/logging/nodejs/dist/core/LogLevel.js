"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.LogLevelValue = exports.LogLevel = void 0;
/**
 * Log levels supported by the logger
 */
var LogLevel;
(function (LogLevel) {
    LogLevel["DEBUG"] = "DEBUG";
    LogLevel["INFO"] = "INFO";
    LogLevel["WARNING"] = "WARNING";
    LogLevel["ERROR"] = "ERROR";
    LogLevel["CRITICAL"] = "CRITICAL";
})(LogLevel || (exports.LogLevel = LogLevel = {}));
/**
 * Numeric values for log level comparison
 */
exports.LogLevelValue = {
    [LogLevel.DEBUG]: 0,
    [LogLevel.INFO]: 1,
    [LogLevel.WARNING]: 2,
    [LogLevel.ERROR]: 3,
    [LogLevel.CRITICAL]: 4
};
//# sourceMappingURL=LogLevel.js.map