import { verify, VerifyOptions } from 'jsonwebtoken';
import { PrimusIdentityOptions, JwtPayload, PrimusUser } from './types';

/**
 * Validates configuration options
 * @throws {Error} If required fields are missing or invalid
 */
export function validateOptions(options: PrimusIdentityOptions): void {
  if (!options.portalUrl) {
    throw new Error('portalUrl is required');
  }

  if (!options.clientId) {
    throw new Error('clientId is required');
  }

  if (!options.clientSecret) {
    throw new Error('clientSecret is required');
  }

  if (!options.jwtSecret) {
    throw new Error('jwtSecret is required');
  }

  try {
    new URL(options.portalUrl);
  } catch {
    throw new Error('portalUrl must be a valid URL');
  }
}

/**
 * Applies default values to options
 */
export function applyDefaults(options: PrimusIdentityOptions): Required<PrimusIdentityOptions> {
  return {
    ...options,
    issuer: options.issuer || options.portalUrl,
    audience: options.audience || options.clientId,
    validateLifetime: options.validateLifetime ?? true,
    clockSkew: options.clockSkew ?? 300,
  };
}

/**
 * Validates a JWT token
 * @throws {Error} If token is invalid
 */
export function validateToken(token: string, options: Required<PrimusIdentityOptions>): JwtPayload {
  const verifyOptions: VerifyOptions = {
    issuer: options.issuer,
    audience: options.audience,
    ignoreExpiration: !options.validateLifetime,
    clockTolerance: options.clockSkew,
  };

  try {
    const decoded = verify(token, options.jwtSecret, verifyOptions);
    return decoded as JwtPayload;
  } catch (error) {
    if (error instanceof Error) {
      throw new Error(`Token validation failed: ${error.message}`);
    }
    throw new Error('Token validation failed: Unknown error');
  }
}

/**
 * Extracts user information from JWT payload
 */
export function extractUser(payload: JwtPayload): PrimusUser {
  const roles = Array.isArray(payload.role)
    ? payload.role
    : payload.role
    ? [payload.role]
    : [];

  const additionalClaims: Record<string, string> = {};
  const standardClaims = ['sub', 'email', 'name', 'role', 'iss', 'aud', 'exp', 'iat', 'nbf', 'jti'];

  for (const [key, value] of Object.entries(payload)) {
    if (!standardClaims.includes(key) && typeof value === 'string') {
      additionalClaims[key] = value;
    }
  }

  return {
    userId: payload.sub || '',
    email: payload.email || '',
    name: payload.name || '',
    roles,
    additionalClaims,
  };
}
