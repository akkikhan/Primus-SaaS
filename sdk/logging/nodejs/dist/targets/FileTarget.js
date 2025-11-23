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
/**
 * Target that writes logs to a file
 */
class FileTarget {
    constructor(options) {
        this.options = options;
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
    write(logEntry) {
        if (this.stream.writable) {
            const line = JSON.stringify(logEntry) + '\n';
            this.stream.write(line);
        }
    }
    close() {
        return new Promise((resolve) => {
            if (this.stream && !this.stream.destroyed) {
                this.stream.end(() => {
                    resolve();
                });
            }
            else {
                resolve();
            }
        });
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