import { Request, Response, NextFunction } from 'express';
import { PrimusIdentityOptions, PrimusUser } from './types';
import { PrimusIdentityValidator, extractUser } from './validator';

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
export function primusIdentityMiddleware(
  options: PrimusIdentityOptions
): (req: Request, res: Response, next: NextFunction) => void {
  const validator = new PrimusIdentityValidator(options);

  return async (req: Request, res: Response, next: NextFunction): Promise<void> => {
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
      const result = await validator.validateToken(token);

      if (!result.isValid) {
        res.status(401).json({ error: result.error || 'Token validation failed' });
        return;
      }

      // Extract user information
      const user = extractUser(result.claims!);

      // Attach user to request
      req.primusUser = user;

      // Attach tenant context if available
      if (result.tenantContext) {
        (req as any).tenantContext = result.tenantContext;
      }

      next();
    } catch (error) {
      if (error instanceof Error) {
        res.status(401).json({ error: error.message });
      } else {
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
export function requireRoles(
  ...roles: string[]
): (req: Request, res: Response, next: NextFunction) => void {
  return (req: Request, res: Response, next: NextFunction): void => {
    if (!req.primusUser) {
      res.status(401).json({ error: 'User not authenticated' });
      return;
    }

    const hasRequiredRole = roles.some((role) => req.primusUser!.roles.includes(role));

    if (!hasRequiredRole) {
      res.status(403).json({ error: 'Insufficient permissions', requiredRoles: roles });
      return;
    }

    next();
  };
}
