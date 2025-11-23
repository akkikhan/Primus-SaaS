import { Target } from './Target';
import { LogEntry } from '../core/LogEntry';
import { LogLevel } from '../core/LogLevel';

/**
 * Target that writes logs to the console
 * Supports pretty printing for development
 */
export class ConsoleTarget implements Target {
    constructor(private options: { pretty?: boolean } = {}) { }

    write(logEntry: LogEntry): void {
        if (this.options.pretty) {
            this.prettyPrint(logEntry);
        } else {
            console.log(JSON.stringify(logEntry));
        }
    }

    private prettyPrint(entry: LogEntry): void {
        const color = this.getColor(entry.level);
        const reset = '\x1b[0m';
        const gray = '\x1b[90m';

        const timestamp = new Date(entry.timestamp).toLocaleTimeString();
        const contextStr = Object.keys(entry.context).length > 0
            ? `\n${gray}${JSON.stringify(entry.context, null, 2)}${reset}`
            : '';

        console.log(
            `${gray}[${timestamp}]${reset} ${color}${entry.level}${reset}: ${entry.message}${contextStr}`
        );
    }

    private getColor(level: LogLevel): string {
        switch (level) {
            case LogLevel.DEBUG: return '\x1b[36m'; // Cyan
            case LogLevel.INFO: return '\x1b[32m'; // Green
            case LogLevel.WARNING: return '\x1b[33m'; // Yellow
            case LogLevel.ERROR: return '\x1b[31m'; // Red
            case LogLevel.CRITICAL: return '\x1b[41m\x1b[37m'; // White on Red
            default: return '\x1b[37m'; // White
        }
    }
}
