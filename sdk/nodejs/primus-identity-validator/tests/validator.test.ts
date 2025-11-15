import { validateOptions, applyDefaults, validateToken, extractUser } from '../src/validator';
import { PrimusIdentityOptions, JwtPayload } from '../src/types';
import { sign } from 'jsonwebtoken';

describe('validateOptions', () => {
  it('should validate correct options', () => {
    const options: PrimusIdentityOptions = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
    };

    expect(() => validateOptions(options)).not.toThrow();
  });

  it('should throw when portalUrl is missing', () => {
    const options = {
      clientId: 'test-client',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
    } as PrimusIdentityOptions;

    expect(() => validateOptions(options)).toThrow('portalUrl is required');
  });

  it('should throw when clientId is missing', () => {
    const options = {
      portalUrl: 'https://portal.primus-saas.com',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
    } as PrimusIdentityOptions;

    expect(() => validateOptions(options)).toThrow('clientId is required');
  });

  it('should throw when clientSecret is missing', () => {
    const options = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      jwtSecret: 'test-jwt-secret',
    } as PrimusIdentityOptions;

    expect(() => validateOptions(options)).toThrow('clientSecret is required');
  });

  it('should throw when jwtSecret is missing', () => {
    const options = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      clientSecret: 'test-secret',
    } as PrimusIdentityOptions;

    expect(() => validateOptions(options)).toThrow('jwtSecret is required');
  });

  it('should throw when portalUrl is invalid', () => {
    const options: PrimusIdentityOptions = {
      portalUrl: 'not-a-valid-url',
      clientId: 'test-client',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
    };

    expect(() => validateOptions(options)).toThrow('portalUrl must be a valid URL');
  });
});

describe('applyDefaults', () => {
  it('should apply default values', () => {
    const options: PrimusIdentityOptions = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
    };

    const result = applyDefaults(options);

    expect(result.issuer).toBe('https://portal.primus-saas.com');
    expect(result.audience).toBe('test-client');
    expect(result.validateLifetime).toBe(true);
    expect(result.clockSkew).toBe(300);
  });

  it('should preserve custom values', () => {
    const options: PrimusIdentityOptions = {
      portalUrl: 'https://portal.primus-saas.com',
      clientId: 'test-client',
      clientSecret: 'test-secret',
      jwtSecret: 'test-jwt-secret',
      issuer: 'custom-issuer',
      audience: 'custom-audience',
      validateLifetime: false,
      clockSkew: 600,
    };

    const result = applyDefaults(options);

    expect(result.issuer).toBe('custom-issuer');
    expect(result.audience).toBe('custom-audience');
    expect(result.validateLifetime).toBe(false);
    expect(result.clockSkew).toBe(600);
  });
});

describe('validateToken', () => {
  const options: Required<PrimusIdentityOptions> = {
    portalUrl: 'https://portal.primus-saas.com',
    clientId: 'test-client',
    clientSecret: 'test-secret',
    jwtSecret: 'test-jwt-secret-key',
    issuer: 'https://portal.primus-saas.com',
    audience: 'test-client',
    validateLifetime: true,
    clockSkew: 300,
  };

  it('should validate a correct token', () => {
    const payload = {
      sub: 'user-123',
      email: 'test@example.com',
      name: 'Test User',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, options.jwtSecret, { expiresIn: '1h' });

    const result = validateToken(token, options);

    expect(result.sub).toBe('user-123');
    expect(result.email).toBe('test@example.com');
  });

  it('should throw for invalid signature', () => {
    const payload = {
      sub: 'user-123',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, 'wrong-secret', { expiresIn: '1h' });

    expect(() => validateToken(token, options)).toThrow('Token validation failed');
  });

  it('should throw for expired token when validateLifetime is true', () => {
    const payload = {
      sub: 'user-123',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, options.jwtSecret, { expiresIn: '-1h' });

    expect(() => validateToken(token, options)).toThrow('Token validation failed');
  });

  it('should accept expired token when validateLifetime is false', () => {
    const modifiedOptions = { ...options, validateLifetime: false };
    const payload = {
      sub: 'user-123',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, options.jwtSecret, { expiresIn: '-1h' });

    const result = validateToken(token, modifiedOptions);
    expect(result.sub).toBe('user-123');
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
