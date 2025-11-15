"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.primusIdentityMiddleware = primusIdentityMiddleware;
exports.requireRoles = requireRoles;
const validator_1 = require("./validator");
/**
 * Creates an Express middleware for Primus identity validation
 * @param options Configuration options
 * @returns Express middleware function
 */
function primusIdentityMiddleware(options) {
    (0, validator_1.validateOptions)(options);
    const fullOptions = (0, validator_1.applyDefaults)(options);
    return (req, res, next) => {
        try {
            // Extract token from Authorization header
            const authHeader = req.headers.authorization;
            if (!authHeader) {
                res.status(401).json({ error: 'Missing Authorization header' });
                return;
            }
            if (!authHeader.startsWith('Bearer ')) {
                res.status(401).json({ error: 'Invalid Authorization header format. Expected: Bearer <token>' });
                return;
            }
            const token = authHeader.substring(7);
            if (!token) {
                res.status(401).json({ error: 'Missing token in Authorization header' });
                return;
            }
            // Validate token
            const payload = (0, validator_1.validateToken)(token, fullOptions);
            // Extract user information
            const user = (0, validator_1.extractUser)(payload);
            // Attach user to request
            req.primusUser = user;
            next();
        }
        catch (error) {
            if (error instanceof Error) {
                res.status(401).json({ error: error.message });
            }
            else {
                res.status(401).json({ error: 'Authentication failed' });
            }
        }
    };
}
/**
 * Creates a middleware that requires specific roles
 * @param roles Required roles
 * @returns Express middleware function
 */
function requireRoles(...roles) {
    return (req, res, next) => {
        if (!req.primusUser) {
            res.status(401).json({ error: 'User not authenticated' });
            return;
        }
        const hasRequiredRole = roles.some((role) => req.primusUser.roles.includes(role));
        if (!hasRequiredRole) {
            res.status(403).json({ error: 'Insufficient permissions', requiredRoles: roles });
            return;
        }
        next();
    };
}
//# sourceMappingURL=express.js.map