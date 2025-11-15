import { PrimusIdentityOptions, JwtPayload, PrimusUser, ValidationMode, TokenValidationResult } from './types';
import { LocalValidator } from './validators/localValidator';
import { AzureAdValidator } from './validators/azureAdValidator';

/**
 * Main validator class for Primus SaaS identity validation
 */
export class PrimusIdentityValidator {
  private readonly options: PrimusIdentityOptions;
  private readonly localValidator: LocalValidator;
  private readonly azureAdValidator: AzureAdValidator;

  constructor(options: PrimusIdentityOptions) {
    this.validateOptions(options);
    this.options = this.applyDefaults(options);
    this.localValidator = new LocalValidator();
    this.azureAdValidator = new AzureAdValidator();
  }

  /**
   * Validates a JWT token based on configured mode
   * @param token JWT token to validate
   * @returns Token validation result
   */
  async validateToken(token: string): Promise<TokenValidationResult> {
    const mode = this.options.mode ?? ValidationMode.Local;

    switch (mode) {
      case ValidationMode.Local:
        return this.validateLocalToken(token);

      case ValidationMode.AzureAd:
        return this.validateAzureAdToken(token);

      case ValidationMode.Hybrid: {
        // Try Azure AD first, fallback to Local
        const azureResult = await this.validateAzureAdToken(token);
        if (azureResult.isValid) {
          return azureResult;
        }
        return this.validateLocalToken(token);
      }

      default:
        throw new Error(`Unsupported validation mode: ${mode}`);
    }
  }

  /**
   * Validates a token using Local JWT validation
   */
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

  /**
   * Validates a token using Azure AD validation
   */
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

  /**
   * Validates configuration options
   * @throws {Error} If required fields are missing or invalid
   */
  private validateOptions(options: PrimusIdentityOptions): void {
    if (!options.portalUrl) {
      throw new Error('portalUrl is required');
    }

    if (!options.clientId) {
      throw new Error('clientId is required');
    }

    if (!options.clientSecret) {
      throw new Error('clientSecret is required');
    }

    const mode = options.mode ?? ValidationMode.Local;

    // Validate mode-specific requirements
    if (mode === ValidationMode.Local || mode === ValidationMode.Hybrid) {
      if (!options.jwtSecret) {
        throw new Error('jwtSecret is required for Local and Hybrid modes');
      }
    }

    if (mode === ValidationMode.AzureAd || mode === ValidationMode.Hybrid) {
      if (!options.tenantId) {
        throw new Error('tenantId is required for AzureAd and Hybrid modes');
      }
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
  private applyDefaults(options: PrimusIdentityOptions): PrimusIdentityOptions {
    return {
      ...options,
      mode: options.mode ?? ValidationMode.Local,
      issuer: options.issuer || options.portalUrl,
      audience: options.audience || options.clientId,
      validateLifetime: options.validateLifetime ?? true,
      clockSkew: options.clockSkew ?? 300,
      jwksCacheTtl: options.jwksCacheTtl ?? 24
    };
  }

  /**
   * Clears all caches
   */
  clearCache(): void {
    this.azureAdValidator.clearCache();
  }
}

/**
 * Validates a JWT token (backward compatible function)
 * @param token JWT token to validate
 * @param options Validation options
 * @returns Token validation result
 */
export async function validateToken(
  token: string,
  options: PrimusIdentityOptions
): Promise<TokenValidationResult> {
  const validator = new PrimusIdentityValidator(options);
  return validator.validateToken(token);
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
