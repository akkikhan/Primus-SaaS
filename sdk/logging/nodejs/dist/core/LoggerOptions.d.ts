import { LogLevel } from './LogLevel';
/**
 * Target configuration for output destinations
 */
export interface TargetConfig {
    type: 'console' | 'file' | 'application-insights';
    [key: string]: any;
}
/**
 * Masking configuration for PII protection
 */
export interface MaskingConfig {
    enabled: boolean;
    fields: string[];
    customFields?: string[];
    strategy?: 'redact' | 'hash' | 'partial';
}
/**
 * Buffering configuration for async writes
 */
export interface BufferingConfig {
    enabled: boolean;
    bufferSize: number;
    flushInterval: number;
    flushOnExit: boolean;
}
/**
 * Performance monitoring configuration
 */
export interface PerformanceConfig {
    enabled: boolean;
    logSlowWrites: boolean;
    slowWriteThreshold: number;
}
/**
 * Logger configuration options
 */
export interface LoggerOptions {
    /** Application ID from Primus Portal (required) */
    applicationId: string;
    /** Environment (required) */
    environment: 'development' | 'testing' | 'production';
    /** Minimum log level (default: INFO) */
    minLevel?: LogLevel;
    /** Output targets */
    targets?: TargetConfig[];
    /** PII masking configuration */
    masking?: MaskingConfig;
    /** Buffering configuration */
    buffering?: BufferingConfig;
    /** Performance monitoring */
    performance?: PerformanceConfig;
}
//# sourceMappingURL=LoggerOptions.d.ts.map