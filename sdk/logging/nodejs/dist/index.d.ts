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
export declare function createLogger(options: LoggerOptions): Logger;
export { Logger, LogLevel, LoggerOptions, LogEntry, Context, RequestEnricher, UserEnricher, TenantEnricher, Target, ConsoleTarget, FileTarget, primusLoggingMiddleware };
export type { Timer } from './core/Logger';
//# sourceMappingURL=index.d.ts.map