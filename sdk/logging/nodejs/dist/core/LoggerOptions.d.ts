import { LogLevel } from './LogLevel';
import { Enricher } from '../enrichers/RequestEnricher';
/**
 * Target configuration for output destinations
 */
export interface TargetConfig {
    type: 'console' | 'file' | 'application-insights' | 'applicationInsights';
    pretty?: boolean;
    path?: string;
    async?: boolean;
    maxFileSize?: number;
    maxRetainedFiles?: number;
    compressRotatedFiles?: boolean;
    connectionString?: string;
    roleName?: string;
}
/**
 * Masking configuration for PII protection
 */
export interface MaskingConfig {
    enabled?: boolean;
    fields?: string[];
    customFields?: string[];
    strategy?: 'redact' | 'hash' | 'partial';
    maskEmails?: boolean;
    maskCreditCards?: boolean;
    maskSSN?: boolean;
    customSensitiveKeys?: string[];
}
/**
 * Buffering configuration for async writes
 */
export interface BufferingConfig {
    enabled?: boolean;
    bufferSize?: number;
    flushInterval?: number;
    flushIntervalMs?: number;
    flushOnExit?: boolean;
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
    /** Custom enrichers executed after built-in enrichers */
    enrichers?: Enricher[];
}
//# sourceMappingURL=LoggerOptions.d.ts.map