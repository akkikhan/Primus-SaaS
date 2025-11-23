import * as fs from 'fs';
import * as path from 'path';
import { Target } from './Target';
import { LogEntry } from '../core/LogEntry';

export interface FileTargetOptions {
    path: string;
    // Rotation options will be added in Milestone 3
}

/**
 * Target that writes logs to a file
 */
export class FileTarget implements Target {
    private stream: fs.WriteStream;

    constructor(private options: FileTargetOptions) {
        this.ensureDirectoryExists(options.path);

        // Explicitly create file to ensure it exists before stream creation
        if (!fs.existsSync(options.path)) {
            fs.writeFileSync(options.path, '');
        }

        this.stream = fs.createWriteStream(options.path, { flags: 'a' });

        // Handle stream errors to prevent process crash
        this.stream.on('error', (err) => {
            console.error(`FileTarget stream error (${options.path}):`, err);
        });
    }

    write(logEntry: LogEntry): void {
        if (this.stream.writable) {
            const line = JSON.stringify(logEntry) + '\n';
            this.stream.write(line);
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

    private ensureDirectoryExists(filePath: string): void {
        const dir = path.dirname(filePath);
        if (!fs.existsSync(dir)) {
            fs.mkdirSync(dir, { recursive: true });
        }
    }
}
