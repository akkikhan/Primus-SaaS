import { Target } from './Target';
import { LogEntry } from '../core/LogEntry';
/**
 * Target that writes logs to the console
 * Supports pretty printing for development
 */
export declare class ConsoleTarget implements Target {
    private options;
    constructor(options?: {
        pretty?: boolean;
    });
    write(logEntry: LogEntry): void;
    private prettyPrint;
    private getColor;
}
//# sourceMappingURL=ConsoleTarget.d.ts.map