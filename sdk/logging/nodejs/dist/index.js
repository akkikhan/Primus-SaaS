"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.LogLevel = exports.Logger = void 0;
exports.createLogger = createLogger;
const Logger_1 = require("./core/Logger");
Object.defineProperty(exports, "Logger", { enumerable: true, get: function () { return Logger_1.Logger; } });
const LogLevel_1 = require("./core/LogLevel");
Object.defineProperty(exports, "LogLevel", { enumerable: true, get: function () { return LogLevel_1.LogLevel; } });
/**
 * Creates a new logger instance
 */
function createLogger(options) {
    return new Logger_1.Logger(options);
}
//# sourceMappingURL=index.js.map