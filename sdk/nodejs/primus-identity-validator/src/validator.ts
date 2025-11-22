import * as jwt from 'jsonwebtoken';
import { PrimusIdentityOptions, TokenValidationResult, IssuerConfig, PrimusUser } from './types';
import { LocalValidator } from './validators/localValidator';
import { AzureAdValidator } from './validators/azureAdValidator';

import { JwksClient } from './services/jwksClient';

/**
 * Main validator class for Primus SaaS identity validation
 */
export class PrimusIdentityValidator {
  private readonly options: PrimusIdentityOptions;
  private readonly localValidator: LocalValidator;
  private readonly azureAdValidator: AzureAdValidator;
  private readonly jwksClient: JwksClient;

  constructor(options: PrimusIdentityOptions) {
    this.validateOptions(options);
    this.options = this.applyDefaults(options);
    this.localValidator = new LocalValidator();
    this.azureAdValidator = new AzureAdValidator();
    this.jwksClient = new JwksClient(options.jwksCacheTtl);
  }

  /**
   * Validates a JWT token based on configured issuers
   * @param token JWT token to validate
   * @returns Token validation result
   */
  async validateToken(token: string): Promise<TokenValidationResult> {
    try {
      // 1. Decode token to find issuer
      const decoded = jwt.decode(token, { complete: true });

      if (!decoded || typeof decoded === 'string') {
        return { isValid: false, error: 'Invalid token format' };
      }

      const payload = decoded.payload as jwt.JwtPayload;
      const issuer = payload.iss;

      if (!issuer) {
        return { isValid: false, error: 'Token missing issuer (iss) claim' };
      }

      // 2. Find matching issuer configuration
      const issuerConfig = this.options.issuers.find(i => i.issuer === issuer);

      if (!issuerConfig) {
        return {
          isValid: false,
          error: `Untrusted issuer: ${issuer}. No matching configuration found.`
        };
      }

      let result: TokenValidationResult;

      // 3. Route to appropriate validator
      if (issuerConfig.type === 'oidc') {
        result = await this.validateOidcToken(token, issuerConfig);
      } else if (issuerConfig.type === 'jwt') {
        result = await this.validateJwtToken(token, issuerConfig);
      } else {
        return { isValid: false, error: `Unsupported issuer type: ${(issuerConfig as any).type}` };
      }

      // 4. Resolve Tenant Context
      if (result.isValid && result.claims && this.options.tenantResolver) {
        try {
          const tenantContext = await this.options.tenantResolver(result.claims);
          result.tenantContext = tenantContext;
        } catch (error) {
          console.error('Primus Identity: Tenant resolution failed', error);
          // We don't fail validation if resolution fails, but we might want to log it
          // Or should we fail? The spec implies it's part of the pipeline.
          // If resolution fails, the user probably shouldn't be allowed in if they rely on it.
          // But let's keep it safe and just log for now, or maybe fail?
          // Let's fail if resolution fails, as it's likely critical.
          return { isValid: false, error: `Tenant resolution failed: ${error instanceof Error ? error.message : String(error)}` };
        }
      }

      return result;

    } catch (error) {
      return {
        isValid: false,
        error: error instanceof Error ? error.message : 'Unknown validation error'
      };
    }
  }

  /**
   * Validates a token using OIDC (Azure AD) validation
   */
  private async validateOidcToken(token: string, config: IssuerConfig): Promise<TokenValidationResult> {
    if (!config.authority) {
      return { isValid: false, error: `Authority URL required for OIDC issuer: ${config.name}` };
    }

    // Extract Tenant ID from Authority URL
    // Expected format: https://login.microsoftonline.com/<tenant-id>/v2.0
    const tenantId = this.extractTenantId(config.authority);

    if (!tenantId) {
      return { isValid: false, error: `Could not extract Tenant ID from authority: ${config.authority}` };
    }

    // Use the first audience for now (TODO: Update AzureAdValidator to support array)
    const audience = config.audiences[0];

    return this.azureAdValidator.validateTokenAsync(token, {
      tenantId: tenantId,
      audience: audience,
      validateLifetime: this.options.validateLifetime ?? true,
      clockSkew: this.options.clockSkew
    });
  }

  /**
   * Validates a token using Local JWT validation
   */
  private async validateJwtToken(token: string, config: IssuerConfig): Promise<TokenValidationResult> {
    // Use the first audience for now
    const audience = config.audiences[0];

    if (config.jwksUrl) {
      try {
        // Get kid from token header
        const decoded = jwt.decode(token, { complete: true });
        const kid = decoded && typeof decoded !== 'string' ? decoded.header.kid : undefined;

        // Fetch signing key
        const signingKey = await this.jwksClient.getSigningKey(config.jwksUrl, kid);

        // Validate using the key
        return new Promise((resolve) => {
          jwt.verify(token, signingKey, {
            algorithms: ['RS256'],
            audience: audience,
            issuer: config.issuer,
            ignoreExpiration: !(this.options.validateLifetime ?? true),
            clockTolerance: this.options.clockSkew
          }, (err, decoded) => {
            if (err) {
              resolve({ isValid: false, error: err.message });
            } else {
              resolve({ isValid: true, claims: decoded as Record<string, unknown> });
            }
          });
        });
      } catch (error) {
        return { isValid: false, error: `JWKS validation failed: ${error instanceof Error ? error.message : String(error)}` };
      }
    } else if (config.secret) {
      return this.localValidator.validateTokenAsync(token, {
        secret: config.secret,
        issuer: config.issuer,
        audience: audience,
        validateLifetime: this.options.validateLifetime ?? true,
        clockSkew: this.options.clockSkew
      });
    } else {
      return { isValid: false, error: `Secret or JWKS URL required for JWT issuer: ${config.name}` };
    }
  }

  private extractTenantId(authority: string): string | null {
    try {
      const url = new URL(authority);
      const parts = url.pathname.split('/').filter(p => p);
      // Handle https://login.microsoftonline.com/<tenant-id>/v2.0
      if (parts.length >= 1) {
        // Check if first part is a GUID-like string
        if (parts[0].match(/^[0-9a-fA-F-]{36}$/)) {
          return parts[0];
        }
        // Handle common variations if needed
      }
      return null;
    } catch {
      return null;
    }
  }

  /**
   * Validates configuration options
   */
  private validateOptions(options: PrimusIdentityOptions): void {
    if (!options.issuers || !Array.isArray(options.issuers) || options.issuers.length === 0) {
      throw new Error('At least one issuer configuration is required');
    }

    for (const issuer of options.issuers) {
      if (!issuer.name) throw new Error('Issuer name is required');
      if (!issuer.type) throw new Error(`Issuer type is required for ${issuer.name}`);
      if (!issuer.issuer) throw new Error(`Issuer claim value is required for ${issuer.name}`);
      if (!issuer.audiences || issuer.audiences.length === 0) throw new Error(`At least one audience is required for ${issuer.name}`);

      if (issuer.type === 'oidc' && !issuer.authority) {
        throw new Error(`Authority URL is required for OIDC issuer ${issuer.name}`);
      }

      if (issuer.type === 'jwt' && !issuer.secret && !issuer.jwksUrl) {
        throw new Error(`Secret or JWKS URL is required for JWT issuer ${issuer.name}`);
      }
    }
  }

  /**
   * Applies default values to options
   */
  private applyDefaults(options: PrimusIdentityOptions): PrimusIdentityOptions {
    return {
      ...options,
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
 * Extracts user information from JWT payload
 */
export function extractUser(claims: any): PrimusUser {
  const roles = Array.isArray(claims.role)
    ? claims.role
    : claims.role
      ? [claims.role]
      : [];

  // Filter out standard claims to get additional claims
  const standardClaims = ['iss', 'sub', 'aud', 'exp', 'nbf', 'iat', 'jti', 'role', 'name', 'email'];
  const additionalClaims = Object.keys(claims)
    .filter(key => !standardClaims.includes(key))
    .reduce((obj, key) => {
      obj[key] = claims[key];
      return obj;
    }, {} as Record<string, any>);

  return {
    userId: claims.sub || '',
    email: claims.email || '',
    name: claims.name || '',
    roles,
    additionalClaims
  };
}
