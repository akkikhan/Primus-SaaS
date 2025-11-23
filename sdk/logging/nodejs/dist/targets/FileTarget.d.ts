import { Target } from './Target';
import { LogEntry } from '../core/LogEntry';
export interface FileTargetOptions {
    path: string;
}
/**
 * Target that writes logs to a file
 */
export declare class FileTarget implements Target {
    private options;
    private stream;
    constructor(options: FileTargetOptions);
    write(logEntry: LogEntry): void;
    close(): Promise<void>;
    private ensureDirectoryExists;
}
//# sourceMappingURL=FileTarget.d.ts.map