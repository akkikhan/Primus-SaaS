"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.ApplicationInsightsTarget = void 0;
const applicationinsights_1 = __importStar(require("applicationinsights"));
const LogLevel_1 = require("../core/LogLevel");
/**
 * Sends structured logs to Azure Application Insights.
 */
class ApplicationInsightsTarget {
    constructor(options) {
        const client = applicationinsights_1.default.defaultClient ?? new applicationinsights_1.TelemetryClient(options.connectionString);
        if (!applicationinsights_1.default.defaultClient) {
            applicationinsights_1.default
                .setup(options.connectionString)
                .setAutoCollectRequests(false)
                .setAutoCollectPerformance(false, false)
                .setAutoCollectExceptions(false)
                .setAutoCollectDependencies(false)
                .setAutoDependencyCorrelation(false)
                .setAutoCollectConsole(false)
                .setUseDiskRetryCaching(false)
                .start();
        }
        client.config.connectionString = options.connectionString;
        this.client = client;
        if (options.roleName) {
            this.client.context.tags[this.client.context.keys.cloudRole] = options.roleName;
        }
    }
    write(logEntry) {
        try {
            this.client.trackTrace({
                message: logEntry.message,
                severity: mapSeverity(logEntry.level),
                properties: {
                    ...logEntry.context,
                    level: logEntry.level,
                    timestamp: logEntry.timestamp
                }
            });
        }
        catch (err) {
            console.error('ApplicationInsightsTarget failed to send log', err);
        }
    }
}
exports.ApplicationInsightsTarget = ApplicationInsightsTarget;
function mapSeverity(level) {
    switch (level) {
        case LogLevel_1.LogLevel.DEBUG:
            return applicationinsights_1.KnownSeverityLevel.Verbose;
        case LogLevel_1.LogLevel.INFO:
            return applicationinsights_1.KnownSeverityLevel.Information;
        case LogLevel_1.LogLevel.WARNING:
            return applicationinsights_1.KnownSeverityLevel.Warning;
        case LogLevel_1.LogLevel.ERROR:
            return applicationinsights_1.KnownSeverityLevel.Error;
        case LogLevel_1.LogLevel.CRITICAL:
            return applicationinsights_1.KnownSeverityLevel.Critical;
        default:
            return applicationinsights_1.KnownSeverityLevel.Information;
    }
}
//# sourceMappingURL=ApplicationInsightsTarget.js.map