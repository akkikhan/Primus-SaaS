import { Target } from './Target';
import { LogEntry } from '../core/LogEntry';
export interface FileTargetOptions {
    path: string;
    maxFileSize?: number;
    maxRetainedFiles?: number;
    compressRotatedFiles?: boolean;
}
/**
 * Target that writes logs to a file with optional rotation and compression.
 * Uses synchronous file I/O for determinism and to mirror the NuGet behavior.
 */
export declare class FileTarget implements Target {
    private options;
    private readonly maxFileSize?;
    private readonly maxRetainedFiles;
    private readonly compressRotatedFiles;
    constructor(options: FileTargetOptions);
    write(logEntry: LogEntry): Promise<void>;
    close(): Promise<void>;
    private rotateIfNeeded;
    private rotateFiles;
    private ensureDirectoryExists;
}
//# sourceMappingURL=FileTarget.d.ts.map