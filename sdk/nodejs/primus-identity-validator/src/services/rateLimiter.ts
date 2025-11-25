import { RateLimitingOptions } from '../types';

export interface ResolvedRateLimitingOptions {
  enabled: boolean;
  maxFailuresPerWindow: number;
  maxGlobalFailuresPerWindow: number;
  windowMs: number;
}

/**
 * Simple in-memory rate limiter for failed token validations.
 * Mirrors the behavior of the .NET middleware (per-client window plus optional global ceiling).
 */
export class FailedValidationRateLimiter {
  private readonly buckets = new Map<string, SlidingWindowCounter>();
  private readonly globalKey = '__global__';
  private readonly retryAfterSeconds: number;

  constructor(private readonly options: ResolvedRateLimitingOptions) {
    this.retryAfterSeconds = Math.max(1, Math.ceil(options.windowMs / 1000));
  }

  registerFailure(clientKey?: string): { limited: boolean; retryAfterSeconds: number } {
    if (!this.options.enabled) {
      return { limited: false, retryAfterSeconds: this.retryAfterSeconds };
    }

    const now = Date.now();
    const key = clientKey || this.globalKey;

    const clientCounter = this.getCounter(key);
    const clientCount = clientCounter.increment(now, this.options.windowMs);

    if (clientCount > this.options.maxFailuresPerWindow) {
      return { limited: true, retryAfterSeconds: this.retryAfterSeconds };
    }

    if (this.options.maxGlobalFailuresPerWindow > 0) {
      const globalCounter = this.getCounter(this.globalKey);
      const globalCount = globalCounter.increment(now, this.options.windowMs);
      if (globalCount > this.options.maxGlobalFailuresPerWindow) {
        return { limited: true, retryAfterSeconds: this.retryAfterSeconds };
      }
    }

    return { limited: false, retryAfterSeconds: this.retryAfterSeconds };
  }

  private getCounter(key: string): SlidingWindowCounter {
    const existing = this.buckets.get(key);
    if (existing) {
      return existing;
    }
    const counter = new SlidingWindowCounter();
    this.buckets.set(key, counter);
    return counter;
  }
}

class SlidingWindowCounter {
  private count = 0;
  private windowStart = Date.now();

  increment(now: number, windowMs: number): number {
    if (now - this.windowStart >= windowMs) {
      this.windowStart = now;
      this.count = 0;
    }

    this.count += 1;
    return this.count;
  }
}

export function resolveRateLimitingOptions(options?: RateLimitingOptions): ResolvedRateLimitingOptions {
  const resolved: ResolvedRateLimitingOptions = {
    enabled: options?.enabled ?? false,
    maxFailuresPerWindow: options?.maxFailuresPerWindow ?? 20,
    maxGlobalFailuresPerWindow: options?.maxGlobalFailuresPerWindow ?? 0,
    windowMs: (options?.windowSeconds ?? 60) * 1000
  };

  if (resolved.maxFailuresPerWindow <= 0) {
    resolved.maxFailuresPerWindow = 1;
  }

  if (resolved.maxGlobalFailuresPerWindow < 0) {
    resolved.maxGlobalFailuresPerWindow = 0;
  }

  if (resolved.windowMs <= 0) {
    resolved.windowMs = 60_000;
  }

  return resolved;
}
