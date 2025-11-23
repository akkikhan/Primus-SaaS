import { LogEntry } from '../core/LogEntry';
/**
 * Interface for log output targets
 */
export interface Target {
    /**
     * Write a log entry to the target
     */
    write(logEntry: LogEntry): void | Promise<void>;
    /**
     * Close the target and release resources
     */
    close?(): void | Promise<void>;
}
//# sourceMappingURL=Target.d.ts.map