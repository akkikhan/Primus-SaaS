"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ConsoleTarget = void 0;
const LogLevel_1 = require("../core/LogLevel");
/**
 * Target that writes logs to the console
 * Supports pretty printing for development
 */
class ConsoleTarget {
    constructor(options = {}) {
        this.options = options;
    }
    write(logEntry) {
        if (this.options.pretty) {
            this.prettyPrint(logEntry);
        }
        else {
            console.log(JSON.stringify(logEntry));
        }
    }
    prettyPrint(entry) {
        const color = this.getColor(entry.level);
        const reset = '\x1b[0m';
        const gray = '\x1b[90m';
        const timestamp = new Date(entry.timestamp).toLocaleTimeString();
        const contextStr = Object.keys(entry.context).length > 0
            ? `\n${gray}${JSON.stringify(entry.context, null, 2)}${reset}`
            : '';
        console.log(`${gray}[${timestamp}]${reset} ${color}${entry.level}${reset}: ${entry.message}${contextStr}`);
    }
    getColor(level) {
        switch (level) {
            case LogLevel_1.LogLevel.DEBUG: return '\x1b[36m'; // Cyan
            case LogLevel_1.LogLevel.INFO: return '\x1b[32m'; // Green
            case LogLevel_1.LogLevel.WARNING: return '\x1b[33m'; // Yellow
            case LogLevel_1.LogLevel.ERROR: return '\x1b[31m'; // Red
            case LogLevel_1.LogLevel.CRITICAL: return '\x1b[41m\x1b[37m'; // White on Red
            default: return '\x1b[37m'; // White
        }
    }
}
exports.ConsoleTarget = ConsoleTarget;
//# sourceMappingURL=ConsoleTarget.js.map