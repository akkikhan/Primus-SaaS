import { Enricher } from './RequestEnricher';
/**
 * Enricher that adds user context
 * Integrates with Primus Identity Validator
 */
export declare class UserEnricher implements Enricher {
    enrich(context: Record<string, any>, request?: any): Record<string, any>;
    /**
     * Extract user context from request
     * Supports multiple sources:
     * 1. Primus Identity Validator (req.primusUser)
     * 2. Passport.js (req.user)
     * 3. Custom (req.user or req.auth)
     */
    private extractUserContext;
}
//# sourceMappingURL=UserEnricher.d.ts.map