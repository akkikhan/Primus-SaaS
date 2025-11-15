import { JsonWebKeySet, JsonWebKey } from '../types';
import * as crypto from 'crypto';

interface CachedJwks {
  keySet: JsonWebKeySet;
  cachedAt: Date;
  expiresAt: Date;
}

/**
 * Cache for JSON Web Key Sets with automatic expiry
 */
export class JwksCache {
  private readonly cache: Map<string, CachedJwks>;
  private readonly defaultTtl: number; // milliseconds

  constructor(defaultTtl: number = 24 * 60 * 60 * 1000) {
    this.cache = new Map();
    this.defaultTtl = defaultTtl;
  }

  /**
   * Gets a cached JWKS if it exists and hasn't expired
   * @param cacheKey Cache key (typically the JWKS URI)
   * @returns Cached JWKS or null if not found or expired
   */
  get(cacheKey: string): JsonWebKeySet | null {
    const cached = this.cache.get(cacheKey);
    if (!cached) {
      return null;
    }

    if (Date.now() < cached.expiresAt.getTime()) {
      return cached.keySet;
    }

    // Expired - remove from cache
    this.cache.delete(cacheKey);
    return null;
  }

  /**
   * Stores a JWKS in the cache
   * @param cacheKey Cache key (typically the JWKS URI)
   * @param keySet JWKS to cache
   * @param ttl Optional TTL in milliseconds (defaults to constructor value)
   */
  set(cacheKey: string, keySet: JsonWebKeySet, ttl?: number): void {
    const effectiveTtl = ttl ?? this.defaultTtl;
    
    this.cache.set(cacheKey, {
      keySet,
      cachedAt: new Date(),
      expiresAt: new Date(Date.now() + effectiveTtl)
    });
  }

  /**
   * Clears all cached JWKS
   */
  clear(): void {
    this.cache.clear();
  }

  /**
   * Gets the number of cached JWKS
   */
  get count(): number {
    return this.cache.size;
  }

  /**
   * Converts a JWK to PEM format for use with jsonwebtoken
   * @param jwk JSON Web Key
   * @returns PEM-encoded public key
   */
  static jwkToPem(jwk: JsonWebKey): string {
    if (jwk.kty !== 'RSA') {
      throw new Error('Only RSA keys are supported');
    }

    if (!jwk.n || !jwk.e) {
      throw new Error('Missing modulus or exponent in JWK');
    }

    try {
      // Create RSA public key using Node.js crypto module
      const key = crypto.createPublicKey({
        key: {
          kty: 'RSA',
          n: jwk.n,
          e: jwk.e
        },
        format: 'jwk'
      });

      return key.export({ type: 'spki', format: 'pem' }).toString();
    } catch (error) {
      throw new Error(`Failed to convert JWK to PEM: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  /**
   * Gets all public keys from a JWKS as a map of kid -> PEM
   * @param keySet JSON Web Key Set
   * @returns Map of key ID to PEM-encoded public key
   */
  static getPublicKeys(keySet: JsonWebKeySet): Map<string, string> {
    const keys = new Map<string, string>();

    for (const jwk of keySet.keys) {
      if (jwk.use === 'sig' && jwk.kty === 'RSA') {
        try {
          const pem = this.jwkToPem(jwk);
          keys.set(jwk.kid, pem);
        } catch (err) {
          console.warn(`Failed to convert JWK ${jwk.kid} to PEM:`, err);
        }
      }
    }

    return keys;
  }
}
