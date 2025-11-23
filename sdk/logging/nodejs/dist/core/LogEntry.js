"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.createLogEntry = createLogEntry;
/**
 * Creates a log entry
 */
function createLogEntry(level, message, context = {}) {
    return {
        timestamp: new Date().toISOString(),
        level,
        message,
        context
    };
}
//# sourceMappingURL=LogEntry.js.map