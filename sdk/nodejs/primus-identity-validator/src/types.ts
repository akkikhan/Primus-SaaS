/**
 * Validation mode for JWT token validation
 */
export enum ValidationMode {
  /**
   * Local JWT validation using symmetric key (HMAC)
   */
  Local = 'Local',
  
  /**
   * Azure AD token validation using asymmetric keys (RSA)
   */
  AzureAd = 'AzureAd',
  
  /**
   * Hybrid mode - try Azure AD first, fallback to Local
   */
  Hybrid = 'Hybrid'
}

/**
 * Configuration options for Primus SaaS identity validation
 */
export interface PrimusIdentityOptions {
  /**
   * The base URL of the Primus SaaS Portal
   * @example "https://portal.primus-saas.com"
   */
  portalUrl: string;

  /**
   * The client ID issued by Primus SaaS Portal
   */
  clientId: string;

  /**
   * The client secret issued by Primus SaaS Portal
   */
  clientSecret: string;

  /**
   * Validation mode
   * @default ValidationMode.Local
   */
  mode?: ValidationMode;

  /**
   * Azure AD tenant ID (required for AzureAd and Hybrid modes)
   * @example "cbd15a9b-cd52-4ccc-916a-00e2edb13043"
   */
  tenantId?: string;

  /**
   * JWKS cache TTL in hours
   * @default 24
   */
  jwksCacheTtl?: number;

  /**
   * The JWT secret key for token validation (required for Local and Hybrid modes)
   */
  jwtSecret?: string;

  /**
   * The expected issuer of JWT tokens
   * @default portalUrl
   */
  issuer?: string;

  /**
   * The expected audience of JWT tokens
   * @default clientId
   */
  audience?: string;

  /**
   * Whether to validate token expiration
   * @default true
   */
  validateLifetime?: boolean;

  /**
   * Clock tolerance in seconds for token expiration validation
   * @default 300 (5 minutes)
   */
  clockSkew?: number;
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
}

/**
 * Represents a user authenticated via Primus SaaS Portal
 */
export interface PrimusUser {
  /**
   * The unique user ID from Primus SaaS Portal
   */
  userId: string;

  /**
   * The user's email address
   */
  email: string;

  /**
   * The user's full name
   */
  name: string;

  /**
   * The roles assigned to the user
   */
  roles: string[];

  /**
   * Additional claims from the JWT token
   */
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
