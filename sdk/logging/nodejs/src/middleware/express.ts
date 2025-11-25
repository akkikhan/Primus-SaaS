import { Request, Response, NextFunction } from 'express';
import { Logger } from '../core/Logger';

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
export function primusLoggingMiddleware(logger: Logger) {
    return (req: Request, res: Response, next: NextFunction) => {
        // Attach request to logger so default enrichers can access it
        logger.setRequest(req);

        // Generate or extract request ID
        const requestId = (req.headers['x-request-id'] as string) ||
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
        if ((req as any).user) {
            const user = (req as any).user;
            Object.assign(requestContext, {
                userId: user.id || user.userId || user.sub,
                userEmail: user.email
            });
        }

        // Attach enriched logger to request
        (req as any).logger = {
            debug: (message: string, context?: Record<string, any>) =>
                logger.debug(message, { ...requestContext, ...context }),
            info: (message: string, context?: Record<string, any>) =>
                logger.info(message, { ...requestContext, ...context }),
            warn: (message: string, context?: Record<string, any>) =>
                logger.warn(message, { ...requestContext, ...context }),
            error: (message: string, context?: Record<string, any>) =>
                logger.error(message, { ...requestContext, ...context }),
            critical: (message: string, context?: Record<string, any>) =>
                logger.critical(message, { ...requestContext, ...context }),
            startTimer: () => logger.startTimer()
        };

        // Log request start
        logger.debug(`${req.method} ${req.path}`, requestContext);

        // Track response
        const startTime = Date.now();

        res.on('finish', () => {
            const duration = Date.now() - startTime;
            const logFn = res.statusCode >= 500
                ? logger.error.bind(logger)
                : res.statusCode >= 400
                    ? logger.warn.bind(logger)
                    : logger.info.bind(logger);

            logFn(`Request completed with status ${res.statusCode}`, {
                ...requestContext,
                statusCode: res.statusCode,
                duration
            });

            logger.clearRequest();
        });

        res.on('close', () => {
            logger.clearRequest();
        });

        next();
    };
}

/**
 * TypeScript declaration for Express Request with logger
 */
declare global {
    namespace Express {
        interface Request {
            logger: {
                debug(message: string, context?: Record<string, any>): void;
                info(message: string, context?: Record<string, any>): void;
                warn(message: string, context?: Record<string, any>): void;
                error(message: string, context?: Record<string, any>): void;
                critical(message: string, context?: Record<string, any>): void;
                startTimer(): any;
            };
        }
    }
}
