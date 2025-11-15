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
     * The JWT secret key for token validation
     */
    jwtSecret: string;
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
//# sourceMappingURL=types.d.ts.map