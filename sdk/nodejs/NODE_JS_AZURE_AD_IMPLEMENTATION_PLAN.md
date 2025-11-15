# Node.js SDK Azure AD Implementation Plan

**Date**: November 15, 2025  
**Priority**: P0 (MVP Blocker)  
**Estimated Effort**: 1-2 weeks  
**Target**: Feature parity with .NET SDK

---

## Executive Summary

The Node.js SDK (`@primus-saas/identity-validator`) currently only supports Local JWT validation with HMAC. This plan outlines the complete port of Azure AD validation from the .NET SDK to achieve feature parity.

### Current State
- ❌ No Azure AD validation
- ❌ No JWKS support
- ❌ No ValidationMode enum
- ❌ No OpenID Connect discovery
- ✅ Only Local JWT with `jsonwebtoken` library

### Target State
- ✅ Azure AD token validation (RS256)
- ✅ JWKS fetching & caching (24-hour TTL)
- ✅ OpenID Connect metadata discovery
- ✅ ValidationMode enum (Local, AzureAd, Hybrid)
- ✅ Comprehensive test suite (15+ scenarios)

---

## Architecture Overview

### Components to Implement

```
@primus-saas/identity-validator/
├── src/
│   ├── types.ts (EXTEND)
│   │   ├── ValidationMode enum (Local, AzureAd, Hybrid)
│   │   ├── PrimusIdentityOptions (add TenantId, JwksCacheTtl)
│   │   ├── OpenIdConfiguration interface
│   │   ├── JsonWebKeySet interface
│   │   └── JsonWebKey interface
│   │
│   ├── services/ (NEW)
│   │   ├── openIdConfigurationService.ts
│   │   ├── jwksService.ts
│   │   └── jwksCache.ts
│   │
│   ├── validators/ (NEW)
│   │   ├── azureAdValidator.ts
│   │   └── localValidator.ts (refactor existing)
│   │
│   └── validator.ts (REFACTOR)
│       └── Mode-aware validation routing
│
└── tests/
    ├── azureAdValidator.test.ts (NEW)
    ├── jwksCache.test.ts (NEW)
    └── integration/ (NEW)
        └── azureAd.integration.test.ts
```

---

## Implementation Tasks

### Phase 1: Type Definitions (1 day)

**File**: `src/types.ts`

#### 1.1 Add ValidationMode Enum
```typescript
export enum ValidationMode {
  Local = 'Local',
  AzureAd = 'AzureAd',
  Hybrid = 'Hybrid'
}
```

#### 1.2 Extend PrimusIdentityOptions
```typescript
export interface PrimusIdentityOptions {
  portalUrl: string;
  clientId: string;
  clientSecret: string;
  
  // Existing - Local JWT
  jwtSecret?: string;
  issuer?: string;
  audience?: string;
  
  // NEW - Azure AD
  mode?: ValidationMode;
  tenantId?: string;
  jwksCacheTtl?: number; // hours, default 24
  
  // Common
  validateLifetime?: boolean;
  clockSkew?: number; // seconds
}
```

#### 1.3 Add Azure AD-Specific Types
```typescript
export interface OpenIdConfiguration {
  issuer: string;
  authorization_endpoint: string;
  token_endpoint: string;
  jwks_uri: string;
  id_token_signing_alg_values_supported: string[];
}

export interface JsonWebKeySet {
  keys: JsonWebKey[];
}

export interface JsonWebKey {
  kty: string; // Key Type (RSA)
  use: string; // Key Use (sig)
  kid: string; // Key ID
  x5t: string; // X.509 Certificate SHA-1 Thumbprint
  n: string;   // Modulus
  e: string;   // Exponent
  x5c?: string[]; // X.509 Certificate Chain
  alg?: string; // Algorithm (RS256)
}

export interface TokenValidationResult {
  isValid: boolean;
  claims?: any;
  error?: string;
}
```

---

### Phase 2: OpenID Configuration Service (1 day)

**File**: `src/services/openIdConfigurationService.ts`

#### 2.1 Core Implementation
```typescript
import axios, { AxiosInstance } from 'axios';
import { OpenIdConfiguration } from '../types';

interface CachedConfiguration {
  configuration: OpenIdConfiguration;
  cachedAt: Date;
  expiresAt: Date;
}

export class OpenIdConfigurationService {
  private readonly httpClient: AxiosInstance;
  private readonly configCache: Map<string, CachedConfiguration>;
  private readonly cacheTtl: number; // milliseconds
  private readonly fetchLock: Map<string, Promise<OpenIdConfiguration>>;

  constructor(cacheTtl: number = 24 * 60 * 60 * 1000) {
    this.httpClient = axios.create({ timeout: 10000 });
    this.configCache = new Map();
    this.cacheTtl = cacheTtl;
    this.fetchLock = new Map();
  }

  async getConfigurationAsync(tenantId: string): Promise<OpenIdConfiguration> {
    const wellKnownUrl = this.getWellKnownUrl(tenantId);
    
    // Check cache
    const cached = this.configCache.get(wellKnownUrl);
    if (cached && Date.now() < cached.expiresAt.getTime()) {
      return cached.configuration;
    }
    
    // Check if fetch is in progress
    const inProgress = this.fetchLock.get(wellKnownUrl);
    if (inProgress) {
      return inProgress;
    }
    
    // Fetch from Azure AD
    const fetchPromise = this.fetchConfiguration(wellKnownUrl);
    this.fetchLock.set(wellKnownUrl, fetchPromise);
    
    try {
      const config = await fetchPromise;
      
      // Cache the configuration
      this.configCache.set(wellKnownUrl, {
        configuration: config,
        cachedAt: new Date(),
        expiresAt: new Date(Date.now() + this.cacheTtl)
      });
      
      return config;
    } finally {
      this.fetchLock.delete(wellKnownUrl);
    }
  }

  private async fetchConfiguration(url: string): Promise<OpenIdConfiguration> {
    const response = await this.httpClient.get<OpenIdConfiguration>(url);
    return response.data;
  }

  private getWellKnownUrl(tenantId: string): string {
    return `https://login.microsoftonline.com/${tenantId}/v2.0/.well-known/openid-configuration`;
  }

  clearCache(): void {
    this.configCache.clear();
  }
}
```

---

### Phase 3: JWKS Service & Cache (2 days)

**File**: `src/services/jwksCache.ts`

#### 3.1 JWKS Cache Implementation
```typescript
import { JsonWebKeySet, JsonWebKey } from '../types';
import * as crypto from 'crypto';

interface CachedJwks {
  keySet: JsonWebKeySet;
  cachedAt: Date;
  expiresAt: Date;
}

export class JwksCache {
  private readonly cache: Map<string, CachedJwks>;
  private readonly defaultTtl: number; // milliseconds

  constructor(defaultTtl: number = 24 * 60 * 60 * 1000) {
    this.cache = new Map();
    this.defaultTtl = defaultTtl;
  }

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

  set(cacheKey: string, keySet: JsonWebKeySet, ttl?: number): void {
    const effectiveTtl = ttl ?? this.defaultTtl;
    
    this.cache.set(cacheKey, {
      keySet,
      cachedAt: new Date(),
      expiresAt: new Date(Date.now() + effectiveTtl)
    });
  }

  clear(): void {
    this.cache.clear();
  }

  get count(): number {
    return this.cache.size;
  }

  /**
   * Converts JWK to PEM format for use with jsonwebtoken/jose
   */
  static jwkToPem(jwk: JsonWebKey): string {
    if (jwk.kty !== 'RSA') {
      throw new Error('Only RSA keys are supported');
    }

    if (!jwk.n || !jwk.e) {
      throw new Error('Missing modulus or exponent in JWK');
    }

    // Decode base64url to buffer
    const modulus = Buffer.from(jwk.n, 'base64url');
    const exponent = Buffer.from(jwk.e, 'base64url');

    // Create RSA public key
    const key = crypto.createPublicKey({
      key: {
        kty: 'RSA',
        n: jwk.n,
        e: jwk.e
      },
      format: 'jwk'
    });

    return key.export({ type: 'spki', format: 'pem' }).toString();
  }

  /**
   * Gets all public keys as PEM strings
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
```

**File**: `src/services/jwksService.ts`

#### 3.2 JWKS Service Implementation
```typescript
import axios, { AxiosInstance } from 'axios';
import { JsonWebKeySet } from '../types';
import { JwksCache } from './jwksCache';

export class JwksService {
  private readonly httpClient: AxiosInstance;
  private readonly cache: JwksCache;
  private readonly fetchLock: Map<string, Promise<JsonWebKeySet>>;

  constructor(cache?: JwksCache) {
    this.httpClient = axios.create({ timeout: 10000 });
    this.cache = cache ?? new JwksCache();
    this.fetchLock = new Map();
  }

  async getJwksAsync(jwksUri: string): Promise<JsonWebKeySet> {
    // Try cache first
    const cached = this.cache.get(jwksUri);
    if (cached) {
      return cached;
    }

    // Check if fetch is in progress
    const inProgress = this.fetchLock.get(jwksUri);
    if (inProgress) {
      return inProgress;
    }

    // Fetch from Azure AD
    const fetchPromise = this.fetchJwks(jwksUri);
    this.fetchLock.set(jwksUri, fetchPromise);

    try {
      const keySet = await fetchPromise;
      
      if (!keySet.keys || keySet.keys.length === 0) {
        throw new Error('No keys found in JWKS');
      }

      // Cache the key set
      this.cache.set(jwksUri, keySet);

      return keySet;
    } finally {
      this.fetchLock.delete(jwksUri);
    }
  }

  async getJwksForTenantAsync(tenantId: string): Promise<JsonWebKeySet> {
    const jwksUri = `https://login.microsoftonline.com/${tenantId}/discovery/v2.0/keys`;
    return this.getJwksAsync(jwksUri);
  }

  private async fetchJwks(uri: string): Promise<JsonWebKeySet> {
    const response = await this.httpClient.get<JsonWebKeySet>(uri);
    return response.data;
  }

  clearCache(): void {
    this.cache.clear();
  }
}
```

---

### Phase 4: Azure AD Validator (2 days)

**File**: `src/validators/azureAdValidator.ts`

#### 4.1 Core Validator Implementation
```typescript
import * as jwt from 'jsonwebtoken';
import { OpenIdConfigurationService } from '../services/openIdConfigurationService';
import { JwksService } from '../services/jwksService';
import { JwksCache } from '../services/jwksCache';
import { TokenValidationResult } from '../types';

export interface AzureAdValidationOptions {
  tenantId: string;
  audience: string;
  validateLifetime?: boolean;
  clockSkew?: number; // seconds
}

export class AzureAdValidator {
  private readonly configService: OpenIdConfigurationService;
  private readonly jwksService: JwksService;

  constructor(
    configService?: OpenIdConfigurationService,
    jwksService?: JwksService
  ) {
    this.configService = configService ?? new OpenIdConfigurationService();
    this.jwksService = jwksService ?? new JwksService();
  }

  async validateTokenAsync(
    token: string,
    options: AzureAdValidationOptions
  ): Promise<TokenValidationResult> {
    try {
      if (!token) {
        throw new Error('Token cannot be null or empty');
      }

      if (!options.tenantId) {
        throw new Error('Tenant ID cannot be null or empty');
      }

      if (!options.audience) {
        throw new Error('Audience cannot be null or empty');
      }

      // Decode token header to get kid
      const decoded = jwt.decode(token, { complete: true });
      if (!decoded || typeof decoded === 'string') {
        throw new Error('Invalid token format');
      }

      const kid = decoded.header.kid;
      if (!kid) {
        throw new Error('Token missing kid (key ID) in header');
      }

      // Get OpenID configuration
      const config = await this.configService.getConfigurationAsync(options.tenantId);

      // Get JWKS
      const jwks = await this.jwksService.getJwksAsync(config.jwks_uri);
      const publicKeys = JwksCache.getPublicKeys(jwks);

      // Find the matching key
      const publicKey = publicKeys.get(kid);
      if (!publicKey) {
        throw new Error(`No matching key found for kid: ${kid}`);
      }

      // Validate token
      const validIssuers = this.getValidIssuers(options.tenantId);
      const clockSkew = options.clockSkew ?? 300; // 5 minutes default

      const verifyOptions: jwt.VerifyOptions = {
        algorithms: ['RS256'],
        audience: options.audience,
        issuer: validIssuers,
        clockTolerance: clockSkew,
        ignoreExpiration: !options.validateLifetime
      };

      const payload = jwt.verify(token, publicKey, verifyOptions);

      // Additional validation: ensure tenant ID matches
      const tokenPayload = typeof payload === 'string' ? JSON.parse(payload) : payload;
      const tokenTenantId = tokenPayload.tid;

      if (tokenTenantId && tokenTenantId !== options.tenantId) {
        throw new Error(
          `Token tenant ID '${tokenTenantId}' does not match expected tenant ID '${options.tenantId}'`
        );
      }

      return {
        isValid: true,
        claims: tokenPayload
      };
    } catch (error) {
      return {
        isValid: false,
        error: error instanceof Error ? error.message : 'Unknown validation error'
      };
    }
  }

  private getValidIssuers(tenantId: string): string[] {
    return [
      `https://login.microsoftonline.com/${tenantId}/v2.0`,
      `https://login.microsoftonline.com/${tenantId}/`,
      `https://sts.windows.net/${tenantId}/`
    ];
  }
}
```

---

### Phase 5: Refactor Main Validator (1 day)

**File**: `src/validators/localValidator.ts` (extracted from existing)

```typescript
import * as jwt from 'jsonwebtoken';
import { TokenValidationResult } from '../types';

export interface LocalValidationOptions {
  secret: string;
  issuer?: string;
  audience?: string;
  validateLifetime?: boolean;
  clockSkew?: number;
}

export class LocalValidator {
  async validateTokenAsync(
    token: string,
    options: LocalValidationOptions
  ): Promise<TokenValidationResult> {
    try {
      const verifyOptions: jwt.VerifyOptions = {
        algorithms: ['HS256'],
        issuer: options.issuer,
        audience: options.audience,
        clockTolerance: options.clockSkew ?? 300,
        ignoreExpiration: !options.validateLifetime
      };

      const payload = jwt.verify(token, options.secret, verifyOptions);

      return {
        isValid: true,
        claims: typeof payload === 'string' ? JSON.parse(payload) : payload
      };
    } catch (error) {
      return {
        isValid: false,
        error: error instanceof Error ? error.message : 'Unknown validation error'
      };
    }
  }
}
```

**File**: `src/validator.ts` (refactored)

```typescript
import { PrimusIdentityOptions, ValidationMode, TokenValidationResult } from './types';
import { LocalValidator } from './validators/localValidator';
import { AzureAdValidator } from './validators/azureAdValidator';

export class PrimusIdentityValidator {
  private readonly options: PrimusIdentityOptions;
  private readonly localValidator: LocalValidator;
  private readonly azureAdValidator: AzureAdValidator;

  constructor(options: PrimusIdentityOptions) {
    this.options = options;
    this.localValidator = new LocalValidator();
    this.azureAdValidator = new AzureAdValidator();
  }

  async validateToken(token: string): Promise<TokenValidationResult> {
    const mode = this.options.mode ?? ValidationMode.Local;

    switch (mode) {
      case ValidationMode.Local:
        return this.validateLocalToken(token);

      case ValidationMode.AzureAd:
        return this.validateAzureAdToken(token);

      case ValidationMode.Hybrid:
        // Try Azure AD first, fallback to Local
        const azureResult = await this.validateAzureAdToken(token);
        if (azureResult.isValid) {
          return azureResult;
        }
        return this.validateLocalToken(token);

      default:
        throw new Error(`Unsupported validation mode: ${mode}`);
    }
  }

  private async validateLocalToken(token: string): Promise<TokenValidationResult> {
    if (!this.options.jwtSecret) {
      return {
        isValid: false,
        error: 'JWT secret not configured for Local mode'
      };
    }

    return this.localValidator.validateTokenAsync(token, {
      secret: this.options.jwtSecret,
      issuer: this.options.issuer,
      audience: this.options.audience,
      validateLifetime: this.options.validateLifetime ?? true,
      clockSkew: this.options.clockSkew
    });
  }

  private async validateAzureAdToken(token: string): Promise<TokenValidationResult> {
    if (!this.options.tenantId) {
      return {
        isValid: false,
        error: 'Tenant ID not configured for Azure AD mode'
      };
    }

    if (!this.options.clientId) {
      return {
        isValid: false,
        error: 'Client ID not configured for Azure AD mode'
      };
    }

    return this.azureAdValidator.validateTokenAsync(token, {
      tenantId: this.options.tenantId,
      audience: this.options.clientId,
      validateLifetime: this.options.validateLifetime ?? true,
      clockSkew: this.options.clockSkew
    });
  }
}

// Export main validation function (backward compatible)
export async function validateToken(
  token: string,
  options: PrimusIdentityOptions
): Promise<TokenValidationResult> {
  const validator = new PrimusIdentityValidator(options);
  return validator.validateToken(token);
}
```

---

### Phase 6: Test Suite (2 days)

**File**: `tests/azureAdValidator.test.ts`

#### 6.1 Core Test Scenarios
```typescript
import { AzureAdValidator } from '../src/validators/azureAdValidator';
import { OpenIdConfigurationService } from '../src/services/openIdConfigurationService';
import { JwksService } from '../src/services/jwksService';
import nock from 'nock';

describe('AzureAdValidator', () => {
  const mockTenantId = 'test-tenant-id';
  const mockClientId = 'test-client-id';
  const mockAudience = mockClientId;

  beforeEach(() => {
    nock.cleanAll();
  });

  describe('validateTokenAsync', () => {
    it('should validate valid Azure AD token', async () => {
      // Mock OIDC discovery
      nock('https://login.microsoftonline.com')
        .get(`/${mockTenantId}/v2.0/.well-known/openid-configuration`)
        .reply(200, {
          issuer: `https://login.microsoftonline.com/${mockTenantId}/v2.0`,
          jwks_uri: `https://login.microsoftonline.com/${mockTenantId}/discovery/v2.0/keys`
        });

      // Mock JWKS endpoint
      nock('https://login.microsoftonline.com')
        .get(`/${mockTenantId}/discovery/v2.0/keys`)
        .reply(200, {
          keys: [
            {
              kty: 'RSA',
              use: 'sig',
              kid: 'test-kid',
              n: 'test-modulus',
              e: 'AQAB'
            }
          ]
        });

      // Create test token (requires real RSA signing for full test)
      const validator = new AzureAdValidator();
      
      // Test implementation...
    });

    it('should reject token with wrong audience', async () => {
      // Implementation...
    });

    it('should reject expired token', async () => {
      // Implementation...
    });

    it('should reject token from wrong tenant', async () => {
      // Implementation...
    });

    it('should reject token with invalid signature', async () => {
      // Implementation...
    });

    it('should reject token with unsupported algorithm', async () => {
      // Implementation...
    });

    // Add 10+ more scenarios matching .NET SDK test coverage
  });
});
```

---

## Implementation Checklist

### Week 1
- [x] Day 1: Type definitions (ValidationMode, extended options, Azure AD types)
- [ ] Day 2: OpenIdConfigurationService with caching
- [ ] Day 3: JwksCache with PEM conversion
- [ ] Day 4: JwksService with JWKS fetching
- [ ] Day 5: AzureAdValidator core implementation

### Week 2
- [ ] Day 1: Refactor validator.ts for mode routing
- [ ] Day 2: Unit tests for services (OpenIdConfig, JWKS, Cache)
- [ ] Day 3: Unit tests for AzureAdValidator (15+ scenarios)
- [ ] Day 4: Integration tests with mocked Azure AD
- [ ] Day 5: Documentation, examples, final testing

---

## Dependencies

### NPM Packages Required
```json
{
  "dependencies": {
    "jsonwebtoken": "^9.0.2",
    "axios": "^1.6.0"
  },
  "devDependencies": {
    "nock": "^13.4.0",
    "@types/jsonwebtoken": "^9.0.5"
  }
}
```

---

## Testing Strategy

### Unit Tests
- OpenIdConfigurationService: Fetch, cache, expiry
- JwksService: Fetch, cache, key conversion
- JwksCache: Cache operations, TTL, PEM conversion
- AzureAdValidator: All validation scenarios

### Integration Tests
- Mock Azure AD endpoints with `nock`
- Test full validation flow: OIDC → JWKS → Validation
- Test cache behavior under concurrent requests

### E2E Tests
- Test with real Azure AD tokens (manual)
- Test with test app (Node.js backend)

---

## Success Criteria

1. ✅ All unit tests passing (>90% coverage)
2. ✅ Integration tests with mocked Azure AD passing
3. ✅ E2E test with Node.js test app successful
4. ✅ Feature parity with .NET SDK confirmed
5. ✅ Documentation updated
6. ✅ Examples created
7. ✅ NPM package published

---

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Complex RS256 validation in Node.js | High | Use battle-tested `jsonwebtoken` library |
| JWKS key conversion to PEM | Medium | Use Node.js `crypto.createPublicKey` API |
| Cache thread-safety | Low | Node.js single-threaded, use Map |
| Azure AD endpoint changes | Low | Follow .NET SDK pattern, use standard endpoints |

---

## Next Actions

1. **Approve Plan** - Review and approve this implementation plan
2. **Setup Environment** - Ensure Node.js 18+, TypeScript 5+ installed
3. **Create Branch** - `feature/nodejs-azure-ad-validation`
4. **Start Implementation** - Begin with Phase 1 (Type Definitions)
5. **Incremental Testing** - Test each phase before moving to next
6. **Code Review** - Review at Week 1 end, final review at Week 2 end
7. **Publish** - NPM publish after all tests passing
