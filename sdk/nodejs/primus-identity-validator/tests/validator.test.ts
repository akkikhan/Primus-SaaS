import { PrimusIdentityValidator } from '../src/validator';
import { IssuerConfig, IssuerType, PrimusIdentityOptions, TokenValidationResult } from '../src/types';
import { sign } from 'jsonwebtoken';

/** Helper to build options with a single local JWT issuer */
function buildOptions(): PrimusIdentityOptions {
  const localIssuer: IssuerConfig = {
    name: 'LocalAuth',
    type: 'jwt' as IssuerType,
    issuer: 'https://auth.local',
    secret: 'local-secret',
    audiences: ['api://my-api']
  };
  return {
    issuers: [localIssuer],
    clockSkew: 300,
    validateLifetime: true,
    jwksCacheTtl: 24
  };
}

describe('PrimusIdentityValidator – Local JWT flow', () => {
  const validator = new PrimusIdentityValidator(buildOptions());

  it('accepts a valid token', async () => {
    const payload = { iss: 'https://auth.local', aud: 'api://my-api', sub: 'user-1' };
    const token = sign(payload, 'local-secret');
    const result: TokenValidationResult = await validator.validateToken(token);
    expect(result.isValid).toBe(true);
    expect(result.claims?.sub).toBe('user-1');
  });

  it('rejects a token signed with the wrong secret', async () => {
    const payload = { iss: 'https://auth.local', aud: 'api://my-api', sub: 'user-2' };
    const token = sign(payload, 'wrong-secret');
    const result = await validator.validateToken(token);
    expect(result.isValid).toBe(false);
    expect(result.error).toContain('invalid signature');
  });

  it('rejects a token with an unknown issuer', async () => {
    const token = sign({ iss: 'https://unknown.issuer', aud: 'api://my-api', sub: 'x' }, 'any-secret');
    const result = await validator.validateToken(token);
    expect(result.isValid).toBe(false);
    expect(result.error).toContain('Untrusted issuer');
  });
});

describe('PrimusIdentityValidator – Options validation', () => {
  it('throws when issuers array is missing', () => {
    const badOptions: any = { clockSkew: 300 };
    expect(() => new PrimusIdentityValidator(badOptions)).toThrow(
      'At least one issuer configuration is required'
    );
  });
});
