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
export declare function primusLoggingMiddleware(logger: Logger): (req: Request, res: Response, next: NextFunction) => void;
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
//# sourceMappingURL=express.d.ts.map