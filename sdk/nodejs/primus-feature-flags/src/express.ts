import { Request, Response, NextFunction, RequestHandler } from 'express';
import { FeatureFlagService } from './featureFlagService';
import { FeatureFlagContext } from './types';

/**
 * Express middleware options for feature flag checking.
 */
export interface FeatureFlagMiddlewareOptions {
  /**
   * The feature flag name to check.
   */
  featureName: string;

  /**
   * Function to extract user context from the request.
   * Default extracts userId from req.user.id or req.user.sub.
   */
  contextExtractor?: (req: Request) => FeatureFlagContext;

  /**
   * Response to send when feature is disabled.
   * Default sends 404 with { error: 'Feature not available' }.
   */
  disabledResponse?: {
    status: number;
    body: any;
  };
}

/**
 * Creates Express middleware that checks if a feature flag is enabled.
 * @param service The feature flag service instance.
 * @param options Middleware options.
 * @returns Express middleware function.
 */
export function requireFeature(
  service: FeatureFlagService,
  options: FeatureFlagMiddlewareOptions
): RequestHandler {
  const contextExtractor =
    options.contextExtractor ?? defaultContextExtractor;
  const disabledResponse = options.disabledResponse ?? {
    status: 404,
    body: { error: 'Feature not available' },
  };

  return (req: Request, res: Response, next: NextFunction): void => {
    const context = contextExtractor(req);
    const isEnabled = service.isEnabled(options.featureName, context);

    if (isEnabled) {
      next();
    } else {
      res.status(disabledResponse.status).json(disabledResponse.body);
    }
  };
}

/**
 * Default context extractor for Express requests.
 */
function defaultContextExtractor(req: Request): FeatureFlagContext {
  const user = (req as any).user;

  if (!user) {
    return {};
  }

  return {
    userId: user.id ?? user.sub ?? user.userId,
    email: user.email,
    groups: user.groups ?? user.roles ?? [],
  };
}

/**
 * Adds feature flag service to Express request.
 * @param service The feature flag service instance.
 * @returns Express middleware that adds service to req.featureFlags.
 */
export function useFeatureFlags(service: FeatureFlagService): RequestHandler {
  return (req: Request, _res: Response, next: NextFunction): void => {
    (req as any).featureFlags = service;
    next();
  };
}

// Type augmentation for Express
declare global {
  namespace Express {
    interface Request {
      featureFlags?: FeatureFlagService;
    }
  }
}
