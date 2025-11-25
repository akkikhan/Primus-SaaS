"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.FileTarget = void 0;
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const zlib = __importStar(require("zlib"));
/**
 * Target that writes logs to a file with optional rotation and compression.
 * Uses synchronous file I/O for determinism and to mirror the NuGet behavior.
 */
class FileTarget {
    constructor(options) {
        this.options = options;
        this.maxFileSize = options.maxFileSize;
        this.maxRetainedFiles = options.maxRetainedFiles ?? 5;
        this.compressRotatedFiles = options.compressRotatedFiles ?? false;
        this.ensureDirectoryExists(options.path);
        if (!fs.existsSync(options.path)) {
            fs.writeFileSync(options.path, '');
        }
    }
    async write(logEntry) {
        const line = JSON.stringify(logEntry) + '\n';
        const lineLength = Buffer.byteLength(line);
        this.rotateIfNeeded(lineLength);
        fs.appendFileSync(this.options.path, line);
    }
    close() {
        return Promise.resolve();
    }
    rotateIfNeeded(nextLength) {
        if (!this.maxFileSize) {
            return;
        }
        const currentSize = fs.existsSync(this.options.path) ? fs.statSync(this.options.path).size : 0;
        if (currentSize + nextLength <= this.maxFileSize) {
            return;
        }
        this.rotateFiles();
    }
    rotateFiles() {
        // Shift existing rotated files
        for (let i = this.maxRetainedFiles; i >= 1; i--) {
            const rotatedPath = `${this.options.path}.${i}`;
            const nextPath = `${this.options.path}.${i + 1}`;
            if (fs.existsSync(rotatedPath)) {
                if (i === this.maxRetainedFiles) {
                    fs.rmSync(rotatedPath, { force: true });
                }
                else {
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
    ensureDirectoryExists(filePath) {
        const dir = path.dirname(filePath);
        if (!fs.existsSync(dir)) {
            fs.mkdirSync(dir, { recursive: true });
        }
    }
}
exports.FileTarget = FileTarget;
//# sourceMappingURL=FileTarget.js.map