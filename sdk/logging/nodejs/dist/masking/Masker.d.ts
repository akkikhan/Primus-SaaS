import { MaskingConfig } from '../core/LoggerOptions';
export type MaskingStrategy = 'redact' | 'hash' | 'partial';
export interface ResolvedMaskingConfig {
    enabled: boolean;
    strategy: MaskingStrategy;
    maskEmails: boolean;
    maskCreditCards: boolean;
    maskSSN: boolean;
    customSensitiveKeys: string[];
    fields: string[];
}
export declare function resolveMaskingConfig(config?: MaskingConfig): ResolvedMaskingConfig;
/**
 * Applies masking rules to objects, arrays, and primitive values.
 */
export declare class Masker {
    private readonly options;
    constructor(options: ResolvedMaskingConfig);
    mask<T>(value: T): T;
    private maskValue;
    private shouldMaskKey;
    private maskStringIfSensitive;
    private applyStrategy;
}
//# sourceMappingURL=Masker.d.ts.map