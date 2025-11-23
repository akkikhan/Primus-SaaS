import { Enricher } from './RequestEnricher';

/**
 * Enricher that adds user context
 * Integrates with Primus Identity Validator
 */
export class UserEnricher implements Enricher {
    enrich(context: Record<string, any>, request?: any): Record<string, any> {
        if (!request) {
            return context;
        }

        const userContext = this.extractUserContext(request);

        if (userContext) {
            return {
                ...context,
                ...userContext
            };
        }

        return context;
    }

    /**
     * Extract user context from request
     * Supports multiple sources:
     * 1. Primus Identity Validator (req.primusUser)
     * 2. Passport.js (req.user)
     * 3. Custom (req.user or req.auth)
     */
    private extractUserContext(request: any): Record<string, any> | null {
        // Priority 1: Primus Identity Validator
        if (request.primusUser) {
            return {
                userId: request.primusUser.userId || request.primusUser.id,
                userEmail: request.primusUser.email,
                userRoles: request.primusUser.roles
            };
        }

        // Priority 2: Passport.js or generic req.user
        if (request.user) {
            return {
                userId: request.user.id || request.user.userId || request.user.sub,
                userEmail: request.user.email,
                userRoles: request.user.roles
            };
        }

        // Priority 3: Custom auth (req.auth)
        if (request.auth) {
            return {
                userId: request.auth.userId || request.auth.sub,
                userEmail: request.auth.email
            };
        }

        return null;
    }
}
