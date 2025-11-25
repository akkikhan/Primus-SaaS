import * as fs from 'fs';
import * as path from 'path';
import * as zlib from 'zlib';
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
export class FileTarget implements Target {
    private readonly maxFileSize?: number;
    private readonly maxRetainedFiles: number;
    private readonly compressRotatedFiles: boolean;

    constructor(private options: FileTargetOptions) {
        this.maxFileSize = options.maxFileSize;
        this.maxRetainedFiles = options.maxRetainedFiles ?? 5;
        this.compressRotatedFiles = options.compressRotatedFiles ?? false;

        this.ensureDirectoryExists(options.path);

        if (!fs.existsSync(options.path)) {
            fs.writeFileSync(options.path, '');
        }
    }

    async write(logEntry: LogEntry): Promise<void> {
        const line = JSON.stringify(logEntry) + '\n';
        const lineLength = Buffer.byteLength(line);

        this.rotateIfNeeded(lineLength);
        fs.appendFileSync(this.options.path, line);
    }

    close(): Promise<void> {
        return Promise.resolve();
    }

    private rotateIfNeeded(nextLength: number): void {
        if (!this.maxFileSize) {
            return;
        }

        const currentSize = fs.existsSync(this.options.path) ? fs.statSync(this.options.path).size : 0;

        if (currentSize + nextLength <= this.maxFileSize) {
            return;
        }

        this.rotateFiles();
    }

    private rotateFiles(): void {
        // Shift existing rotated files
        for (let i = this.maxRetainedFiles; i >= 1; i--) {
            const rotatedPath = `${this.options.path}.${i}`;
            const nextPath = `${this.options.path}.${i + 1}`;

            if (fs.existsSync(rotatedPath)) {
                if (i === this.maxRetainedFiles) {
                    fs.rmSync(rotatedPath, { force: true });
                } else {
                    fs.renameSync(rotatedPath, nextPath);
                }
            }
        }

        const rotatedFirst = `${this.options.path}.1`;
        if (fs.existsSync(this.options.path)) {
            fs.renameSync(this.options.path, rotatedFirst);
        }

        if (this.compressRotatedFiles && fs.existsSync(rotatedFirst)) {
            const data = fs.readFileSync(rotatedFirst);
            const compressed = zlib.gzipSync(data);
            fs.writeFileSync(`${rotatedFirst}.gz`, compressed);
            fs.rmSync(rotatedFirst, { force: true });
        }

        // Recreate base file
        fs.writeFileSync(this.options.path, '');
    }

    private ensureDirectoryExists(filePath: string): void {
        const dir = path.dirname(filePath);
        if (!fs.existsSync(dir)) {
            fs.mkdirSync(dir, { recursive: true });
        }
    }
}
