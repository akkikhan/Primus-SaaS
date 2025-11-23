import { Enricher } from './RequestEnricher';
/**
 * Enricher that adds tenant context
 * Integrates with Primus Identity Validator for multi-tenant apps
 */
export declare class TenantEnricher implements Enricher {
    enrich(context: Record<string, any>, request?: any): Record<string, any>;
    /**
     * Extract tenant context from request
     * Supports multiple sources:
     * 1. Primus Identity Validator (req.primusTenantContext)
     * 2. Custom tenant header (X-Tenant-ID)
     * 3. Custom (req.tenant)
     */
    private extractTenantContext;
}
//# sourceMappingURL=TenantEnricher.d.ts.map