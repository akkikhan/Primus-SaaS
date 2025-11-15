import { PrimusIdentityValidator, validateToken, extractUser } from '../src/validator';
import { PrimusIdentityOptions, JwtPayload, ValidationMode } from '../src/types';
import { sign } from 'jsonwebtoken';

describe('PrimusIdentityValidator - Options Validation', () => {
  it('should validate correct Local mode options', () => {
    const options: PrimusIdentityOptions = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
      mode: ValidationMode.Local,
    };

    expect(() => new PrimusIdentityValidator(options)).not.toThrow();
  });

  it('should throw when portalUrl is missing', () => {
    const options = {
      clientId: 'test-client',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
    } as PrimusIdentityOptions;

    expect(() => new PrimusIdentityValidator(options)).toThrow('portalUrl is required');
  });

  it('should throw when clientId is missing', () => {
    const options = {
      portalUrl: 'https://portal.primus-saas.com',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
    } as PrimusIdentityOptions;

    expect(() => new PrimusIdentityValidator(options)).toThrow('clientId is required');
  });

  it('should throw when clientSecret is missing', () => {
    const options = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      jwtSecret: 'test-jwt-secret',
    } as PrimusIdentityOptions;

    expect(() => new PrimusIdentityValidator(options)).toThrow('clientSecret is required');
  });

  it('should throw when jwtSecret is missing for Local mode', () => {
    const options = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      clientSecret: 'test-secret',
      mode: ValidationMode.Local,
    } as PrimusIdentityOptions;

    expect(() => new PrimusIdentityValidator(options)).toThrow('jwtSecret is required for Local and Hybrid modes');
  });

  it('should throw when tenantId is missing for AzureAd mode', () => {
    const options = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      clientSecret: 'test-secret',
      mode: ValidationMode.AzureAd,
    } as PrimusIdentityOptions;

    expect(() => new PrimusIdentityValidator(options)).toThrow('tenantId is required for AzureAd and Hybrid modes');
  });

  it('should throw when portalUrl is invalid', () => {
    const options: PrimusIdentityOptions = {
      portalUrl: 'not-a-valid-url',
      clientId: 'test-client',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
    };

    expect(() => new PrimusIdentityValidator(options)).toThrow('portalUrl must be a valid URL');
  });
});

describe('PrimusIdentityValidator - Local Mode Token Validation', () => {
  const options: PrimusIdentityOptions = {
    portalUrl: 'https://portal.primus-saas.com',
    clientId: 'test-client',
    clientSecret: 'test-secret',
    jwtSecret: 'test-jwt-secret-key',
    mode: ValidationMode.Local,
    issuer: 'https://portal.primus-saas.com',
    audience: 'test-client',
    validateLifetime: true,
    clockSkew: 300,
  };

  it('should validate a correct token', async () => {
    const payload = {
      sub: 'user-123',
      email: 'test@example.com',
      name: 'Test User',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, options.jwtSecret!, { expiresIn: '1h' });

    const result = await validateToken(token, options);

    expect(result.isValid).toBe(true);
    expect(result.claims?.sub).toBe('user-123');
    expect(result.claims?.email).toBe('test@example.com');
  });

  it('should reject invalid signature', async () => {
    const payload = {
      sub: 'user-123',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, 'wrong-secret', { expiresIn: '1h' });

    const result = await validateToken(token, options);

    expect(result.isValid).toBe(false);
    expect(result.error).toContain('invalid signature');
  });

  it('should reject expired token when validateLifetime is true', async () => {
    const payload = {
      sub: 'user-123',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, options.jwtSecret!, { expiresIn: '-1h' });

    const result = await validateToken(token, options);

    expect(result.isValid).toBe(false);
    expect(result.error).toContain('expired');
  });

  it('should accept expired token when validateLifetime is false', async () => {
    const modifiedOptions = { ...options, validateLifetime: false };
    const payload = {
      sub: 'user-123',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, options.jwtSecret!, { expiresIn: '-1h' });

    const result = await validateToken(token, modifiedOptions);
    
    expect(result.isValid).toBe(true);
    expect(result.claims?.sub).toBe('user-123');
  });
});

describe('extractUser', () => {
  it('should extract user from payload with all fields', () => {
    const payload: JwtPayload = {
      sub: 'user-123',
      email: 'test@example.com',
      name: 'Test User',
      role: ['Admin', 'User'],
      customClaim: 'custom-value',
    };

    const user = extractUser(payload);

    expect(user.userId).toBe('user-123');
    expect(user.email).toBe('test@example.com');
    expect(user.name).toBe('Test User');
    expect(user.roles).toEqual(['Admin', 'User']);
    expect(user.additionalClaims).toEqual({ customClaim: 'custom-value' });
  });

  it('should handle single role as string', () => {
    const payload: JwtPayload = {
      sub: 'user-123',
      role: 'Admin',
    };

    const user = extractUser(payload);

    expect(user.roles).toEqual(['Admin']);
  });

  it('should handle missing fields', () => {
    const payload: JwtPayload = {};

    const user = extractUser(payload);

    expect(user.userId).toBe('');
    expect(user.email).toBe('');
    expect(user.name).toBe('');
    expect(user.roles).toEqual([]);
    expect(user.additionalClaims).toEqual({});
  });

  it('should filter standard claims from additional claims', () => {
    const payload: JwtPayload = {
      sub: 'user-123',
      iss: 'issuer',
      aud: 'audience',
      exp: 1234567890,
      iat: 1234567890,
      customClaim: 'value',
    };

    const user = extractUser(payload);

    expect(user.additionalClaims).toEqual({ customClaim: 'value' });
    expect(user.additionalClaims).not.toHaveProperty('iss');
    expect(user.additionalClaims).not.toHaveProperty('aud');
  });
});
