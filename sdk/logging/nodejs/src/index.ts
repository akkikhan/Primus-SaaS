import { Logger } from './core/Logger';
import { LogLevel } from './core/LogLevel';
import { LoggerOptions } from './core/LoggerOptions';
import { LogEntry } from './core/LogEntry';

/**
 * Creates a new logger instance
 */
export function createLogger(options: LoggerOptions): Logger {
    return new Logger(options);
}

// Export types
export { Logger, LogLevel, LoggerOptions, LogEntry };

// Export type for Timer
export type { Timer } from './core/Logger';
