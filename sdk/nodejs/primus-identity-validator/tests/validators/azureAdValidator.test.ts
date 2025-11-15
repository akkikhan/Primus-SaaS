import * as jwt from 'jsonwebtoken';
import { AzureAdValidator } from '../../src/validators/azureAdValidator';
import { OpenIdConfigurationService } from '../../src/services/openIdConfigurationService';
import { JwksService } from '../../src/services/jwksService';
import { JwksCache } from '../../src/services/jwksCache';
import { OpenIdConfiguration, JsonWebKeySet } from '../../src/types';

jest.mock('../../src/services/openIdConfigurationService');
jest.mock('../../src/services/jwksService');

describe('AzureAdValidator', () => {
  let validator: AzureAdValidator;
  let mockConfigService: jest.Mocked<OpenIdConfigurationService>;
  let mockJwksService: jest.Mocked<JwksService>;
  
  const testTenantId = 'cbd15a9b-cd52-4ccc-916a-00e2edb13043';
  const testAudience = 'api://test-app';
  const testKid = 'test-key-1';

  // Generate RSA key pair for testing
  const privateKey = `-----BEGIN RSA PRIVATE KEY-----
MIIEpQIBAAKCAQEA7U3ueqcDLVJM0Umh3I170OVZJidu2K4WrtsQrvZmaNRcFpO/
hiYBPMBqNcKd4QW8fn0h2GgFsxV4nXe16EDTrhzIHHf9vh7mmrgaae+wFLPNJVLv
c/WTkntu/1qL092skfTPgsegWXH3J/GP1imHF6SGsJWvnEgtF0MuTGVvEhZSktGD
LGhbvmL7XSuCxpBXkRZ/xRiydfXtGBEEWXAu9QjWSyBZxB8jQTk4jI4yDPM/Czrn
Ua0S1OAXMjkAniLzhrJfC5QLvgSeLjhrhELJv5nHDsamZA6gGLJ4TvIUkkCsccLx
SrCCB0RcmVQrHzcy5r62TviD+Fu2T4Fz3vhXowIDAQABAoIBAEfXRoHSSvdvCXt4
t6xeFuRkcotG99tYRNy1zESzqshEtgZNv3c8zTYXqG/TFdnYUzrV5ieuQj93mhbN
9X+1FICLDRf1SZfDohun7bMxkZSK5GnOxFhOOdloTe+3eUpwOzzKqQHWLpi1lFX+
3wU+eDvznqUaA9qjzSbUO1F8idYm36DtJXNG/AZ0LblK/SUr6BGBsq20A8s1MKof
76429qpzZDQopGu8ZhoNRJe31zgJjLdsMaApK3Jy/ZHkce+qMAXO69ynQYtbzUUH
rA8dO2txGH1hVaXJ7r3UpLo1leSYkdFBleGBwpR1LueABYQSvfjLpCUMPH0dZPYX
RCodMv0CgYEA+vZik71klY5wtvmC/Qo7p4+qNJBSrBTDjUsxinUlWnk2Vlbut512
I7HYD4ickdAmVC0n5aLjM7VQVEhxCwrSv9bz2P/qogVCjPpYrjMZZrkIWaYqiWo0
i3X3tnmzR5ybownKZXkUCxlp7VIorWFsJdYE2FId003DOtbeEbRV6TUCgYEA8hFc
wbaU3q2Dxv4pTPRSgPtoJkkoWfzTSA+zxSO4ml0Zhz5Nvci+BOpYvuOkbb5a4oG6
th5qCKzwgClcJRUfDwx8C7FdobZd156BGh+4bzQpB+iEvuSUlc9Flc3B8cNCY4QK
2oZ5zL/8i5dPjfjVYRUDyeOV4OLuY4sCTorzMHcCgYEA7FqDeJTUWC3wZcMT5HBw
wlyBdkO+OKbNjRKWM8okRdMGo8X3ySIZE4D49Zg5rPIWRwzIGHOR+y712ornO/kT
hu1Qdrx/I9xEUzWrs1QYxi/Zd2URhp1pozaU91wixnzt71zXKz4e7F0g8AuCl9e6
3N91bK3ZvRntlkqqlo36o0kCgYEAq4+Cngg5Glj/vFkGDHRcxO1pszjGUIDPfZMj
ZeUbQMwa+688ifVsNy+gFipgrybqYv5uxgRoQh3PlgW6OM3vZUsD34WlRqdpexfr
iAhTPv1I10IEUwbOCB8Owk1B14b71mwPBTGpwWOQKZ7j00s4b8XlGYEjCG/NZN9w
kxoHoaECgYEA1E0hfkAe6rls0PKMf9uAie7pr6L13FKbMJ+M2Ip9O5SAG/6mUdHm
kdDUE2ocRT6n+pNYfZuzAuUodI3fxzIXxasrfnotbPqEsAx7MWFXH/zczSXEbPWm
9vrn10NGfXJQh7tSsGo6lvJOoaAMSGlxjDyeV+PWWh/f5hHGENDsa+8=
-----END RSA PRIVATE KEY-----`;

  const publicKey = `-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA7U3ueqcDLVJM0Umh3I17
0OVZJidu2K4WrtsQrvZmaNRcFpO/hiYBPMBqNcKd4QW8fn0h2GgFsxV4nXe16EDT
rhzIHHf9vh7mmrgaae+wFLPNJVLvc/WTkntu/1qL092skfTPgsegWXH3J/GP1imH
F6SGsJWvnEgtF0MuTGVvEhZSktGDLGhbvmL7XSuCxpBXkRZ/xRiydfXtGBEEWXAu
9QjWSyBZxB8jQTk4jI4yDPM/CzrnUa0S1OAXMjkAniLzhrJfC5QLvgSeLjhrhELJ
v5nHDsamZA6gGLJ4TvIUkkCsccLxSrCCB0RcmVQrHzcy5r62TviD+Fu2T4Fz3vhX
owIDAQAB
-----END PUBLIC KEY-----`;

  const mockConfig: OpenIdConfiguration = {
    issuer: `https://login.microsoftonline.com/${testTenantId}/v2.0`,
    authorization_endpoint: '',
    token_endpoint: '',
    jwks_uri: `https://login.microsoftonline.com/${testTenantId}/discovery/v2.0/keys`,
    id_token_signing_alg_values_supported: ['RS256']
  };

  const mockJwks: JsonWebKeySet = {
    keys: [{
      kty: 'RSA',
      use: 'sig',
      kid: testKid,
      n: 'xGOr-H7A-PWRUm0lK2JvFnCZNvB7RkHzPUcqNZVmKO2JBh3Rj_7F1Nf3LQM8dO5JniU9D7qJVXdU6yqnJvMn7A',
      e: 'AQAB'
    }]
  };

  beforeEach(() => {
    jest.clearAllMocks();
    
    mockConfigService = {
      getConfigurationAsync: jest.fn().mockResolvedValue(mockConfig),
      clearCache: jest.fn(),
      cacheCount: 0
    } as any;

    mockJwksService = {
      getJwksAsync: jest.fn().mockResolvedValue(mockJwks),
      getJwksForTenantAsync: jest.fn().mockResolvedValue(mockJwks),
      clearCache: jest.fn(),
      cacheCount: 0
    } as any;

    // Mock getPublicKeys to return our test public key
    jest.spyOn(JwksCache, 'getPublicKeys').mockReturnValue(
      new Map([[testKid, publicKey]])
    );

    validator = new AzureAdValidator(mockConfigService, mockJwksService);
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  const createToken = (payload: any, options?: jwt.SignOptions) => {
    return jwt.sign(payload, privateKey, {
      algorithm: 'RS256',
      keyid: testKid,
      ...options
    });
  };

  describe('validateTokenAsync - Valid Tokens', () => {
    it('should validate a valid Azure AD token', async () => {
      const token = createToken({
        sub: 'user-123',
        name: 'Test User',
        email: 'test@example.com',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600,
        iat: Math.floor(Date.now() / 1000)
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(true);
      expect(result.claims).toBeDefined();
      expect(result.claims?.sub).toBe('user-123');
      expect(result.claims?.email).toBe('test@example.com');
      expect(result.error).toBeUndefined();
    });

    it('should accept v1 endpoint issuer format', async () => {
      const v1Issuer = `https://login.microsoftonline.com/${testTenantId}/`;
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: v1Issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(true);
    });

    it('should accept sts.windows.net issuer format', async () => {
      const stsIssuer = `https://sts.windows.net/${testTenantId}/`;
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: stsIssuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(true);
    });

    it('should accept expired token when validateLifetime is false', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) - 3600, // expired 1 hour ago
        iat: Math.floor(Date.now() / 1000) - 7200
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience,
        validateLifetime: false
      });

      expect(result.isValid).toBe(true);
    });

    it('should use custom clock skew', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600,
        iat: Math.floor(Date.now() / 1000)
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience,
        clockSkew: 60 // 1 minute
      });

      expect(result.isValid).toBe(true);
    });
  });

  describe('validateTokenAsync - Invalid Tokens', () => {
    it('should reject empty token', async () => {
      const result = await validator.validateTokenAsync('', {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toContain('Token cannot be null or empty');
    });

    it('should reject token with missing tenant ID', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: '',
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toContain('Tenant ID cannot be null or empty');
    });

    it('should reject token with missing audience', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: ''
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toContain('Audience cannot be null or empty');
    });

    it('should reject token with wrong audience', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: 'wrong-audience',
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should reject expired token by default', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) - 3600, // expired 1 hour ago
        iat: Math.floor(Date.now() / 1000) - 7200
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toContain('expired');
    });

    it('should reject token with wrong tenant ID in tid claim', async () => {
      const wrongTenantId = 'different-tenant-id';
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: wrongTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toContain('tenant ID');
      expect(result.error).toContain(wrongTenantId);
      expect(result.error).toContain(testTenantId);
    });

    it('should reject token with invalid format', async () => {
      const result = await validator.validateTokenAsync('not.a.valid.jwt', {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should reject token with missing kid', async () => {
      // Create token without kid in header
      const token = jwt.sign({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      }, privateKey, { algorithm: 'RS256' }); // no keyid option

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toContain('kid');
    });

    it('should reject token with unknown kid', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      }, { keyid: 'unknown-key-id' });

      // Mock getPublicKeys to return empty map (no matching kid)
      jest.spyOn(JwksCache, 'getPublicKeys').mockReturnValue(new Map());

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toContain('No matching key found');
    });

    it('should reject token with invalid signature', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      // Tamper with the token
      const parts = token.split('.');
      parts[2] = 'tampered-signature';
      const tamperedToken = parts.join('.');

      const result = await validator.validateTokenAsync(tamperedToken, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toBeDefined();
    });

    it('should reject token with wrong issuer', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: 'https://evil.com/',
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      const result = await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(result.isValid).toBe(false);
      expect(result.error).toBeDefined();
    });
  });

  describe('Service Integration', () => {
    it('should fetch OpenID configuration for tenant', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(mockConfigService.getConfigurationAsync).toHaveBeenCalledWith(testTenantId);
    });

    it('should fetch JWKS from jwks_uri', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      expect(mockJwksService.getJwksAsync).toHaveBeenCalledWith(mockConfig.jwks_uri);
    });

    it('should use cached JWKS on subsequent validations', async () => {
      const token = createToken({
        sub: 'user-123',
        aud: testAudience,
        iss: mockConfig.issuer,
        tid: testTenantId,
        exp: Math.floor(Date.now() / 1000) + 3600
      });

      await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      await validator.validateTokenAsync(token, {
        tenantId: testTenantId,
        audience: testAudience
      });

      // Should fetch config and JWKS only once (cached on second call)
      expect(mockConfigService.getConfigurationAsync).toHaveBeenCalledTimes(2);
      expect(mockJwksService.getJwksAsync).toHaveBeenCalledTimes(2);
    });
  });

  describe('clearCache', () => {
    it('should clear all service caches', () => {
      validator.clearCache();

      expect(mockConfigService.clearCache).toHaveBeenCalled();
      expect(mockJwksService.clearCache).toHaveBeenCalled();
    });
  });

  describe('Concurrent Validations', () => {
    it('should handle multiple concurrent validations', async () => {
      const tokens = Array.from({ length: 10 }, (_, i) => 
        createToken({
          sub: `user-${i}`,
          aud: testAudience,
          iss: mockConfig.issuer,
          tid: testTenantId,
          exp: Math.floor(Date.now() / 1000) + 3600
        })
      );

      const results = await Promise.all(
        tokens.map(token => 
          validator.validateTokenAsync(token, {
            tenantId: testTenantId,
            audience: testAudience
          })
        )
      );

      results.forEach((result, i) => {
        expect(result.isValid).toBe(true);
        expect(result.claims?.sub).toBe(`user-${i}`);
      });
    });
  });
});
