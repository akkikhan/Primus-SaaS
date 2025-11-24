"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.primusLoggingMiddleware = primusLoggingMiddleware;
/**
 * Express middleware for automatic HTTP context enrichment
 *
 * @example
 * ```typescript
 * import express from 'express';
 * import { createLogger, primusLoggingMiddleware } from '@primus-saas/logging';
 *
 * const app = express();
 * const logger = createLogger({ applicationId: 'my-app', environment: 'production' });
 *
 * // Add middleware
 * app.use(primusLoggingMiddleware(logger));
 *
 * app.get('/api/users', (req, res) => {
 *   req.logger.info('Fetching users');
 *   res.json({ users: [] });
 * });
 * ```
 */
function primusLoggingMiddleware(logger) {
    return (req, res, next) => {
        // Generate or extract request ID
        const requestId = req.headers['x-request-id'] ||
            `req-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
        // Add request ID to response headers
        res.setHeader('X-Request-ID', requestId);
        // Create enriched context
        const requestContext = {
            requestId,
            method: req.method,
            path: req.path,
            query: req.query,
            ip: req.ip || req.socket.remoteAddress
        };
        // Add user context if available
        if (req.user) {
            const user = req.user;
            Object.assign(requestContext, {
                userId: user.id || user.userId || user.sub,
                userEmail: user.email
            });
        }
        // Attach enriched logger to request
        req.logger = {
            debug: (message, context) => logger.debug(message, { ...requestContext, ...context }),
            info: (message, context) => logger.info(message, { ...requestContext, ...context }),
            warn: (message, context) => logger.warn(message, { ...requestContext, ...context }),
            error: (message, context) => logger.error(message, { ...requestContext, ...context }),
            critical: (message, context) => logger.critical(message, { ...requestContext, ...context }),
            startTimer: () => logger.startTimer()
        };
        // Log request start
        logger.debug(`${req.method} ${req.path}`, requestContext);
        // Track response
        const startTime = Date.now();
        res.on('finish', () => {
            const duration = Date.now() - startTime;
            const level = res.statusCode >= 500 ? 'error' :
                res.statusCode >= 400 ? 'warn' : 'info';
            logger[level](`Request completed with status ${res.statusCode}`, {
                ...requestContext,
                statusCode: res.statusCode,
                duration
            });
        });
        next();
    };
}
//# sourceMappingURL=express.js.map