import { PrimusIdentityOptions, JwtPayload, PrimusUser } from './types';
/**
 * Validates configuration options
 * @throws {Error} If required fields are missing or invalid
 */
export declare function validateOptions(options: PrimusIdentityOptions): void;
/**
 * Applies default values to options
 */
export declare function applyDefaults(options: PrimusIdentityOptions): Required<PrimusIdentityOptions>;
/**
 * Validates a JWT token
 * @throws {Error} If token is invalid
 */
export declare function validateToken(token: string, options: Required<PrimusIdentityOptions>): JwtPayload;
/**
 * Extracts user information from JWT payload
 */
export declare function extractUser(payload: JwtPayload): PrimusUser;
//# sourceMappingURL=validator.d.ts.map