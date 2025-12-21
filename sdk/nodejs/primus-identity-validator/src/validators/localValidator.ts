import * as jwt from 'jsonwebtoken';
import { TokenValidationResult } from '../types';

function normalizeAudience(aud: string | string[] | undefined): string | [string, ...string[]] | undefined {
  if (aud === undefined) return undefined;
  if (typeof aud === 'string') return aud;
  if (aud.length === 0) return undefined;
  if (aud.length === 1) return aud[0];
  return [aud[0], ...aud.slice(1)];
}

export interface LocalValidationOptions {
  secret: string;
  issuer?: string;
  audience?: string | string[];
  validateLifetime?: boolean;
  clockSkew?: number;
}

/**
 * Validator for local JWT tokens using symmetric key (HMAC)
 */
export class LocalValidator {
  /**
   * Validates a local JWT token
   * @param token JWT token to validate
   * @param options Validation options
   * @returns Validation result with claims or error
   */
  async validateTokenAsync(
    token: string,
    options: LocalValidationOptions
  ): Promise<TokenValidationResult> {
    try {
      if (!token) {
        throw new Error('Token cannot be null or empty');
      }

      if (!options.secret) {
        throw new Error('JWT secret cannot be null or empty');
      }

      const verifyOptions: jwt.VerifyOptions = {
        algorithms: ['HS256'],
        issuer: options.issuer,
        audience: normalizeAudience(options.audience),
        clockTolerance: options.clockSkew ?? 300,
        ignoreExpiration: options.validateLifetime === false
      };

      const payload = jwt.verify(token, options.secret, verifyOptions);

      return {
        isValid: true,
        claims: (typeof payload === 'string' ? JSON.parse(payload) : payload) as Record<string, unknown>
      };
    } catch (error) {
      return {
        isValid: false,
        error: error instanceof Error ? error.message : 'Unknown validation error'
      };
    }
  }
}
