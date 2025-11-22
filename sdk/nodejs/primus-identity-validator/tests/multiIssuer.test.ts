import { PrimusIdentityValidator } from '../src/validator';
import { IssuerConfig, IssuerType, PrimusIdentityOptions, TokenValidationResult } from '../src/types';
import { sign } from 'jsonwebtoken';

/** Helper to build options with only a Local JWT issuer */
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

describe('PrimusIdentityValidator – Multi‑Issuer Routing (Local only)', () => {
    const validator = new PrimusIdentityValidator(buildOptions());

    it('validates a proper Local JWT', async () => {
        const payload = { iss: 'https://auth.local', aud: 'api://my-api', sub: 'local-user' };
        const token = sign(payload, 'local-secret');
        const result: TokenValidationResult = await validator.validateToken(token);
        expect(result.isValid).toBe(true);
        expect(result.claims?.sub).toBe('local-user');
    });

    it('rejects token with unknown issuer', async () => {
        const token = sign({ iss: 'https://unknown.issuer', aud: 'api://my-api', sub: 'x' }, 'any-secret');
        const result = await validator.validateToken(token);
        expect(result.isValid).toBe(false);
        expect(result.error).toContain('Untrusted issuer');
    });
});
