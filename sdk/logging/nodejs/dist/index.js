"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.FileTarget = exports.ConsoleTarget = exports.TenantEnricher = exports.UserEnricher = exports.RequestEnricher = exports.Context = exports.LogLevel = exports.Logger = void 0;
exports.createLogger = createLogger;
const Logger_1 = require("./core/Logger");
Object.defineProperty(exports, "Logger", { enumerable: true, get: function () { return Logger_1.Logger; } });
const LogLevel_1 = require("./core/LogLevel");
Object.defineProperty(exports, "LogLevel", { enumerable: true, get: function () { return LogLevel_1.LogLevel; } });
const Context_1 = require("./core/Context");
Object.defineProperty(exports, "Context", { enumerable: true, get: function () { return Context_1.Context; } });
const RequestEnricher_1 = require("./enrichers/RequestEnricher");
Object.defineProperty(exports, "RequestEnricher", { enumerable: true, get: function () { return RequestEnricher_1.RequestEnricher; } });
const UserEnricher_1 = require("./enrichers/UserEnricher");
Object.defineProperty(exports, "UserEnricher", { enumerable: true, get: function () { return UserEnricher_1.UserEnricher; } });
const TenantEnricher_1 = require("./enrichers/TenantEnricher");
Object.defineProperty(exports, "TenantEnricher", { enumerable: true, get: function () { return TenantEnricher_1.TenantEnricher; } });
const ConsoleTarget_1 = require("./targets/ConsoleTarget");
Object.defineProperty(exports, "ConsoleTarget", { enumerable: true, get: function () { return ConsoleTarget_1.ConsoleTarget; } });
const FileTarget_1 = require("./targets/FileTarget");
Object.defineProperty(exports, "FileTarget", { enumerable: true, get: function () { return FileTarget_1.FileTarget; } });
/**
 * Creates a new logger instance
 */
function createLogger(options) {
    return new Logger_1.Logger(options);
}
//# sourceMappingURL=index.js.map