import { Request, Response, NextFunction } from 'express';
import { PrimusIdentityOptions, PrimusUser } from './types';
/**
 * Extends Express Request to include Primus user
 */
declare global {
    namespace Express {
        interface Request {
            primusUser?: PrimusUser;
        }
    }
}
/**
 * Creates an Express middleware for Primus identity validation
 * @param options Configuration options
 * @returns Express middleware function
 */
export declare function primusIdentityMiddleware(options: PrimusIdentityOptions): (req: Request, res: Response, next: NextFunction) => void;
/**
 * Creates a middleware that requires specific roles
 * @param roles Required roles
 * @returns Express middleware function
 */
export declare function requireRoles(...roles: string[]): (req: Request, res: Response, next: NextFunction) => void;
//# sourceMappingURL=express.d.ts.map