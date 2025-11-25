"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Masker = void 0;
exports.resolveMaskingConfig = resolveMaskingConfig;
const crypto_1 = __importDefault(require("crypto"));
const DEFAULT_SENSITIVE_KEYS = [
    'password',
    'pass',
    'secret',
    'token',
    'access_token',
    'refresh_token',
    'authorization',
    'apiKey',
    'apikey',
    'x-api-key',
    'ssn'
];
function resolveMaskingConfig(config) {
    return {
        enabled: config?.enabled ?? false,
        strategy: config?.strategy ?? 'redact',
        maskEmails: config?.maskEmails ?? false,
        maskCreditCards: config?.maskCreditCards ?? false,
        maskSSN: config?.maskSSN ?? false,
        customSensitiveKeys: [
            ...(config?.fields ?? []),
            ...(config?.customFields ?? []),
            ...(config?.customSensitiveKeys ?? [])
        ].map(k => k.toLowerCase()),
        fields: [
            ...(config?.fields ?? []),
            ...(config?.customFields ?? [])
        ].map(k => k.toLowerCase())
    };
}
/**
 * Applies masking rules to objects, arrays, and primitive values.
 */
class Masker {
    constructor(options) {
        this.options = options;
    }
    mask(value) {
        if (!this.options.enabled) {
            return value;
        }
        return this.maskValue(value, undefined);
    }
    maskValue(value, key) {
        if (value === null || value === undefined) {
            return value;
        }
        if (Array.isArray(value)) {
            return value.map(v => this.maskValue(v, key));
        }
        if (typeof value === 'object') {
            const result = {};
            for (const [k, v] of Object.entries(value)) {
                if (this.shouldMaskKey(k)) {
                    result[k] = this.applyStrategy(v);
                }
                else {
                    result[k] = this.maskValue(v, k);
                }
            }
            return result;
        }
        if (typeof value === 'string') {
            if (this.shouldMaskKey(key)) {
                return this.applyStrategy(value);
            }
            const maskedValue = this.maskStringIfSensitive(value);
            return maskedValue;
        }
        return value;
    }
    shouldMaskKey(key) {
        if (!key) {
            return false;
        }
        const lower = key.toLowerCase();
        return this.options.customSensitiveKeys.includes(lower) || DEFAULT_SENSITIVE_KEYS.includes(lower);
    }
    maskStringIfSensitive(value) {
        if (this.options.maskEmails && EMAIL_REGEX.test(value)) {
            return this.applyStrategy(value);
        }
        if (this.options.maskCreditCards && CREDIT_CARD_REGEX.test(value.replace(/[\s-]/g, ''))) {
            return this.applyStrategy(value);
        }
        if (this.options.maskSSN && SSN_REGEX.test(value)) {
            return this.applyStrategy(value);
        }
        return value;
    }
    applyStrategy(value) {
        const str = String(value ?? '');
        switch (this.options.strategy) {
            case 'hash':
                return crypto_1.default.createHash('sha256').update(str).digest('hex').slice(0, 16);
            case 'partial':
                if (str.length <= 4) {
                    return '****';
                }
                const visible = str.slice(-4);
                return `${'*'.repeat(Math.max(4, str.length - 4))}${visible}`;
            case 'redact':
            default:
                return '[REDACTED]';
        }
    }
}
exports.Masker = Masker;
const EMAIL_REGEX = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const CREDIT_CARD_REGEX = /^[0-9]{13,19}$/;
const SSN_REGEX = /^\d{3}-?\d{2}-?\d{4}$/;
//# sourceMappingURL=Masker.js.map