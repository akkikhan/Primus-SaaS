export type IssuerType = 'oidc' | 'jwt';

export interface IssuerConfig {
  /**
   * Friendly name for this issuer (e.g., "AzureAD", "LocalAuth")
   */
  name: string;

  /**
   * Type of issuer
   */
  type: IssuerType;

  /**
   * The 'iss' claim value to match in the token.
   * Used to route the token to the correct validator.
   */
  issuer: string;

  /**
   * For OIDC: The authority URL (e.g., https://login.microsoftonline.com/...)
   */
  authority?: string;

  /**
   * For JWT: The JWKS endpoint URL
   */
  jwksUrl?: string;

  /**
   * For JWT (Local Dev): Shared secret key
   */
  secret?: string;

  /**
   * Valid audiences for this issuer
   */
  audiences: string[];
}

/**
 * Configuration options for Primus SaaS identity validation
 */
export interface PrimusIdentityOptions {
  /**
   * List of trusted identity providers
   */
  issuers: IssuerConfig[];

  /**
   * Global clock skew in seconds (default: 300)
   */
  clockSkew?: number;

  /**
   * Whether to validate token lifetime (default: true)
   */
  validateLifetime?: boolean;

  /**
   * JWKS cache TTL in hours (default: 24)
   */
  /**
   * JWKS cache TTL in hours (default: 24)
   */
  jwksCacheTtl?: number;

  /**
   * Optional function to resolve tenant context from token claims.
   */
  tenantResolver?: (claims: Record<string, unknown>) => TenantContext | Promise<TenantContext>;
}

/**
 * Represents the resolved tenant context.
 */
export interface TenantContext {
  tenantId: string;
  roles: string[];
  metadata?: Record<string, unknown>;
}

/**
 * OpenID Connect discovery document
 */
export interface OpenIdConfiguration {
  issuer: string;
  authorization_endpoint: string;
  token_endpoint: string;
  jwks_uri: string;
  id_token_signing_alg_values_supported: string[];
}

/**
 * JSON Web Key Set
 */
export interface JsonWebKeySet {
  keys: JsonWebKey[];
}

/**
 * JSON Web Key
 */
export interface JsonWebKey {
  kty: string; // Key Type (RSA)
  use: string; // Key Use (sig)
  kid: string; // Key ID
  x5t?: string; // X.509 Certificate SHA-1 Thumbprint (optional)
  n: string;   // Modulus
  e: string;   // Exponent
  x5c?: string[]; // X.509 Certificate Chain
  alg?: string; // Algorithm (RS256)
}

/**
 * Token validation result
 */
export interface TokenValidationResult {
  isValid: boolean;
  claims?: Record<string, unknown>;
  error?: string;
  tenantContext?: TenantContext;
}

/**
 * Represents a user authenticated via Primus SaaS Portal
 */
export interface PrimusUser {
  userId: string;
  email: string;
  name: string;
  roles: string[];
  additionalClaims: Record<string, string>;
}

/**
 * JWT token payload structure
 */
export interface JwtPayload {
  sub?: string;
  email?: string;
  name?: string;
  role?: string | string[];
  iss?: string;
  aud?: string;
  exp?: number;
  iat?: number;
  [key: string]: unknown;
}
