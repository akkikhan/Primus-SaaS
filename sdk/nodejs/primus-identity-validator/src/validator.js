"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.validateOptions = validateOptions;
exports.applyDefaults = applyDefaults;
exports.validateToken = validateToken;
exports.extractUser = extractUser;
const jsonwebtoken_1 = require("jsonwebtoken");
/**
 * Validates configuration options
 * @throws {Error} If required fields are missing or invalid
 */
function validateOptions(options) {
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
    }
    catch {
        throw new Error('portalUrl must be a valid URL');
    }
}
/**
 * Applies default values to options
 */
function applyDefaults(options) {
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
function validateToken(token, options) {
    const verifyOptions = {
        issuer: options.issuer,
        audience: options.audience,
        ignoreExpiration: !options.validateLifetime,
        clockTolerance: options.clockSkew,
    };
    try {
        const decoded = (0, jsonwebtoken_1.verify)(token, options.jwtSecret, verifyOptions);
        return decoded;
    }
    catch (error) {
        if (error instanceof Error) {
            throw new Error(`Token validation failed: ${error.message}`);
        }
        throw new Error('Token validation failed: Unknown error');
    }
}
/**
 * Extracts user information from JWT payload
 */
function extractUser(payload) {
    const roles = Array.isArray(payload.role)
        ? payload.role
        : payload.role
            ? [payload.role]
            : [];
    const additionalClaims = {};
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
//# sourceMappingURL=validator.js.map