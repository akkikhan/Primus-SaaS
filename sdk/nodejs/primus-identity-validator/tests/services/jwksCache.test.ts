import { JwksCache } from '../../src/services/jwksCache';
import { JsonWebKeySet, JsonWebKey } from '../../src/types';

describe('JwksCache', () => {
  let cache: JwksCache;
  
  const mockJwks: JsonWebKeySet = {
    keys: [
      {
        kty: 'RSA',
        use: 'sig',
        kid: 'test-key-1',
        n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JnrCU9D7qJVXdU6yqnJvMn7A',
        e: 'AQAB',
        x5t: 'dGVzdC1rZXktMQ',
        x5c: ['MIIDPjCCAiagAwIBAgIQVWmXY/+9RqFA/OG9kFulHDANBgkqhkiG']
      }
    ]
  };

  beforeEach(() => {
    cache = new JwksCache(1000); // 1 second TTL for testing
  });

  describe('get', () => {
    it('should return null for non-existent key', () => {
      const result = cache.get('non-existent');
      expect(result).toBeNull();
    });

    it('should return cached JWKS if not expired', () => {
      const cacheKey = 'https://test.com/jwks';
      cache.set(cacheKey, mockJwks);

      const result = cache.get(cacheKey);
      expect(result).toEqual(mockJwks);
    });

    it('should return null for expired JWKS', async () => {
      const shortCache = new JwksCache(100); // 100ms TTL
      const cacheKey = 'https://test.com/jwks';
      
      shortCache.set(cacheKey, mockJwks);
      
      // Wait for expiry
      await new Promise(resolve => setTimeout(resolve, 150));
      
      const result = shortCache.get(cacheKey);
      expect(result).toBeNull();
    });

    it('should remove expired entry from cache', async () => {
      const shortCache = new JwksCache(100);
      const cacheKey = 'https://test.com/jwks';
      
      shortCache.set(cacheKey, mockJwks);
      expect(shortCache.count).toBe(1);
      
      await new Promise(resolve => setTimeout(resolve, 150));
      
      shortCache.get(cacheKey);
      expect(shortCache.count).toBe(0);
    });
  });

  describe('set', () => {
    it('should store JWKS with default TTL', () => {
      const cacheKey = 'https://test.com/jwks';
      cache.set(cacheKey, mockJwks);

      expect(cache.count).toBe(1);
      expect(cache.get(cacheKey)).toEqual(mockJwks);
    });

    it('should store JWKS with custom TTL', () => {
      const cacheKey = 'https://test.com/jwks';
      cache.set(cacheKey, mockJwks, 5000); // 5 second TTL

      expect(cache.get(cacheKey)).toEqual(mockJwks);
    });

    it('should overwrite existing entry', () => {
      const cacheKey = 'https://test.com/jwks';
      const updatedJwks: JsonWebKeySet = {
        keys: [
          { ...mockJwks.keys[0], kid: 'updated-key' }
        ]
      };

      cache.set(cacheKey, mockJwks);
      cache.set(cacheKey, updatedJwks);

      expect(cache.count).toBe(1);
      expect(cache.get(cacheKey)).toEqual(updatedJwks);
    });
  });

  describe('clear', () => {
    it('should clear all cached entries', () => {
      cache.set('key1', mockJwks);
      cache.set('key2', mockJwks);
      
      expect(cache.count).toBe(2);

      cache.clear();
      
      expect(cache.count).toBe(0);
      expect(cache.get('key1')).toBeNull();
      expect(cache.get('key2')).toBeNull();
    });
  });

  describe('count', () => {
    it('should return correct count of cached entries', () => {
      expect(cache.count).toBe(0);

      cache.set('key1', mockJwks);
      expect(cache.count).toBe(1);

      cache.set('key2', mockJwks);
      expect(cache.count).toBe(2);

      cache.clear();
      expect(cache.count).toBe(0);
    });
  });

  describe('jwkToPem', () => {
    it('should convert RSA JWK to PEM format', () => {
      const jwk: JsonWebKey = {
        kty: 'RSA',
        use: 'sig',
        kid: 'test-key',
        n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JnrCU9D7qJVXdU6yqnJvMn7A',
        e: 'AQAB'
      };

      const pem = JwksCache.jwkToPem(jwk);

      expect(pem).toContain('-----BEGIN PUBLIC KEY-----');
      expect(pem).toContain('-----END PUBLIC KEY-----');
      expect(typeof pem).toBe('string');
    });

    it('should throw error for non-RSA keys', () => {
      const jwk: any = {
        kty: 'EC', // Elliptic Curve
        use: 'sig',
        kid: 'ec-key',
        crv: 'P-256',
        x: 'test',
        y: 'test',
        n: '', // Not used for EC
        e: ''  // Not used for EC
      };

      expect(() => JwksCache.jwkToPem(jwk)).toThrow('Only RSA keys are supported');
    });

    it('should throw error for missing modulus', () => {
      const jwk: JsonWebKey = {
        kty: 'RSA',
        use: 'sig',
        kid: 'incomplete-key',
        e: 'AQAB',
        n: '' // Empty modulus to test validation
      };

      expect(() => JwksCache.jwkToPem(jwk)).toThrow('Missing modulus or exponent in JWK');
    });

    it('should throw error for missing exponent', () => {
      const jwk: JsonWebKey = {
        kty: 'RSA',
        use: 'sig',
        kid: 'incomplete-key',
        n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JnrCU9D7qJVXdU6yqnJvMn7A',
        e: '' // Empty exponent to test validation
      };

      expect(() => JwksCache.jwkToPem(jwk)).toThrow('Missing modulus or exponent in JWK');
    });
  });

  describe('getPublicKeys', () => {
    it('should extract all RSA signing keys', () => {
      const jwks: JsonWebKeySet = {
        keys: [
          {
            kty: 'RSA',
            use: 'sig',
            kid: 'key1',
            n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JnrCU9D7qJVXdU6yqnJvMn7A',
            e: 'AQAB'
          },
          {
            kty: 'RSA',
            use: 'sig',
            kid: 'key2',
            n: 'yHPs-I8B-QXSVn1mL3KwGoDaOwC8SlI0QVdraO3KCp4Rk_8G2Og4MRN9eP6KosCV-E8rKWYeV7zrnKvNo8Qo8B',
            e: 'AQAB'
          }
        ]
      };

      const keys = JwksCache.getPublicKeys(jwks);

      expect(keys.size).toBe(2);
      expect(keys.has('key1')).toBe(true);
      expect(keys.has('key2')).toBe(true);
      expect(keys.get('key1')).toContain('-----BEGIN PUBLIC KEY-----');
      expect(keys.get('key2')).toContain('-----BEGIN PUBLIC KEY-----');
    });

    it('should filter out non-signing keys', () => {
      const jwks: JsonWebKeySet = {
        keys: [
          {
            kty: 'RSA',
            use: 'sig',
            kid: 'signing-key',
            n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JnrCU9D7qJVXdU6yqnJvMn7A',
            e: 'AQAB'
          },
          {
            kty: 'RSA',
            use: 'enc', // encryption key, not signing
            kid: 'encryption-key',
            n: 'yHPs-I8B-QXSVn1mL3KwGoDaOwC8SlI0QVdraO3KCp4Rk_8G2Og4MRN9eP6KosCV-E8rKWYeV7zrnKvNo8Qo8B',
            e: 'AQAB'
          }
        ]
      };

      const keys = JwksCache.getPublicKeys(jwks);

      expect(keys.size).toBe(1);
      expect(keys.has('signing-key')).toBe(true);
      expect(keys.has('encryption-key')).toBe(false);
    });

    it('should filter out non-RSA keys', () => {
      const jwks: JsonWebKeySet = {
        keys: [
          {
            kty: 'RSA',
            use: 'sig',
            kid: 'rsa-key',
            n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JnrCU9D7qJVXdU6yqnJvMn7A',
            e: 'AQAB'
          },
          {
            kty: 'EC',
            use: 'sig',
            kid: 'ec-key',
            n: '',
            e: ''
          } as any
        ]
      };

      const keys = JwksCache.getPublicKeys(jwks);

      expect(keys.size).toBe(1);
      expect(keys.has('rsa-key')).toBe(true);
      expect(keys.has('ec-key')).toBe(false);
    });

    it('should handle empty key set', () => {
      const jwks: JsonWebKeySet = { keys: [] };
      const keys = JwksCache.getPublicKeys(jwks);

      expect(keys.size).toBe(0);
    });

    it('should skip keys that fail conversion', () => {
      const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation();
      
      const jwks: JsonWebKeySet = {
        keys: [
          {
            kty: 'RSA',
            use: 'sig',
            kid: 'valid-key',
            n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JnrCU9D7qJVXdU6yqnJvMn7A',
            e: 'AQAB'
          },
          {
            kty: 'RSA',
            use: 'sig',
            kid: 'invalid-key',
            n: '',
            e: ''
            // empty n and e - will fail conversion
          }
        ]
      };

      const keys = JwksCache.getPublicKeys(jwks);

      expect(keys.size).toBe(1);
      expect(keys.has('valid-key')).toBe(true);
      expect(keys.has('invalid-key')).toBe(false);
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('Failed to convert JWK invalid-key to PEM'),
        expect.anything()
      );

      consoleWarnSpy.mockRestore();
    });
  });
});
