import { Logger } from './core/Logger';
import { LogLevel } from './core/LogLevel';
import { LoggerOptions } from './core/LoggerOptions';
import { LogEntry } from './core/LogEntry';
import { Context } from './core/Context';
import { RequestEnricher } from './enrichers/RequestEnricher';
import { UserEnricher } from './enrichers/UserEnricher';
import { TenantEnricher } from './enrichers/TenantEnricher';
import { Target } from './targets/Target';
import { ConsoleTarget } from './targets/ConsoleTarget';
import { FileTarget } from './targets/FileTarget';
import { primusLoggingMiddleware } from './middleware/express';

/**
 * Creates a new logger instance
 */
export function createLogger(options: LoggerOptions): Logger {
    return new Logger(options);
}

// Export types and classes
export {
    Logger,
    LogLevel,
    LoggerOptions,
    LogEntry,
    Context,
    RequestEnricher,
    UserEnricher,
    TenantEnricher,
    Target,
    ConsoleTarget,
    FileTarget,
    primusLoggingMiddleware
};

// Export type for Timer
export type { Timer } from './core/Logger';
