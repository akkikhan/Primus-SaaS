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
 */
export class FileTarget implements Target {
    private stream: fs.WriteStream;
    private currentSize: number;
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

        this.currentSize = fs.statSync(options.path).size;
        this.stream = this.createStream();
    }

    async write(logEntry: LogEntry): Promise<void> {
        const line = JSON.stringify(logEntry) + '\n';
        const lineLength = Buffer.byteLength(line);

        await this.rotateIfNeeded(lineLength);

        if (this.stream.writable) {
            this.stream.write(line);
            this.currentSize += lineLength;
        }
    }

    close(): Promise<void> {
        return new Promise((resolve) => {
            if (this.stream && !this.stream.destroyed) {
                this.stream.end(() => {
                    resolve();
                });
            } else {
                resolve();
            }
        });
    }

    private async rotateIfNeeded(nextLength: number): Promise<void> {
        if (!this.maxFileSize) {
            return;
        }

        if (this.currentSize + nextLength <= this.maxFileSize) {
            return;
        }

        await this.rotateFiles();
    }

    private async rotateFiles(): Promise<void> {
        await this.close();

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
            await this.compress(rotatedFirst);
        }

        // Reset base file
        this.stream = this.createStream(true);
        this.currentSize = fs.existsSync(this.options.path) ? fs.statSync(this.options.path).size : 0;
    }

    private async compress(filePath: string): Promise<void> {
        const gzipPath = `${filePath}.gz`;
        return new Promise((resolve, reject) => {
            const source = fs.createReadStream(filePath);
            const destination = fs.createWriteStream(gzipPath);
            const gzip = zlib.createGzip();

            source.pipe(gzip).pipe(destination);

            destination.on('finish', () => {
                fs.rmSync(filePath, { force: true });
                resolve();
            });

            destination.on('error', reject);
        });
    }

    private ensureDirectoryExists(filePath: string): void {
        const dir = path.dirname(filePath);
        if (!fs.existsSync(dir)) {
            fs.mkdirSync(dir, { recursive: true });
        }
    }

    private createStream(truncate: boolean = false): fs.WriteStream {
        const stream = fs.createWriteStream(this.options.path, { flags: truncate ? 'w' : 'a' });
        stream.on('error', (err) => {
            console.error(`FileTarget stream error (${this.options.path}):`, err);
        });
        return stream;
    }
}
