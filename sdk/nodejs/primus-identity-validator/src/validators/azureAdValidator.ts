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

/**
 * Validator for Azure AD JWT tokens
 */
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

  /**
   * Validates an Azure AD JWT token
   * @param token JWT token to validate
   * @param options Validation options
   * @returns Validation result with claims or error
   */
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
        issuer: validIssuers as [string, ...string[]], // Type assertion for tuple
        clockTolerance: clockSkew,
        ignoreExpiration: options.validateLifetime === false
      };

      const payload = jwt.verify(token, publicKey, verifyOptions);

      // Additional validation: ensure tenant ID matches
      const tokenPayload = typeof payload === 'string' ? JSON.parse(payload) : payload;
      const tokenTenantId = tokenPayload.tid as string;

      if (tokenTenantId && tokenTenantId !== options.tenantId) {
        throw new Error(
          `Token tenant ID '${tokenTenantId}' does not match expected tenant ID '${options.tenantId}'`
        );
      }

      return {
        isValid: true,
        claims: tokenPayload as Record<string, unknown>
      };
    } catch (error) {
      return {
        isValid: false,
        error: error instanceof Error ? error.message : 'Unknown validation error'
      };
    }
  }

  /**
   * Gets valid issuers for Azure AD tokens
   * @param tenantId Azure AD tenant ID
   * @returns Array of valid issuer URLs
   */
  private getValidIssuers(tenantId: string): string[] {
    return [
      `https://login.microsoftonline.com/${tenantId}/v2.0`,
      `https://login.microsoftonline.com/${tenantId}/`,
      `https://sts.windows.net/${tenantId}/`
    ];
  }

  /**
   * Clears all caches
   */
  clearCache(): void {
    this.configService.clearCache();
    this.jwksService.clearCache();
  }
}
