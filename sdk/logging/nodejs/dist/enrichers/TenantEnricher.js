"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.TenantEnricher = void 0;
/**
 * Enricher that adds tenant context
 * Integrates with Primus Identity Validator for multi-tenant apps
 */
class TenantEnricher {
    enrich(context, request) {
        if (!request) {
            return context;
        }
        const tenantContext = this.extractTenantContext(request);
        if (tenantContext) {
            return {
                ...context,
                ...tenantContext
            };
        }
        return context;
    }
    /**
     * Extract tenant context from request
     * Supports multiple sources:
     * 1. Primus Identity Validator (req.primusTenantContext)
     * 2. Custom tenant header (X-Tenant-ID)
     * 3. Custom (req.tenant)
     */
    extractTenantContext(request) {
        // Priority 1: Primus Identity Validator
        if (request.primusTenantContext) {
            return {
                tenantId: request.primusTenantContext.tenantId,
                tenantName: request.primusTenantContext.tenantName
            };
        }
        // Priority 2: Custom tenant header
        const headers = request.headers || request.req?.headers || {};
        const tenantIdHeader = headers['x-tenant-id'] || headers['x-tenant'];
        if (tenantIdHeader) {
            return {
                tenantId: tenantIdHeader
            };
        }
        // Priority 3: Custom req.tenant
        if (request.tenant) {
            return {
                tenantId: request.tenant.id || request.tenant.tenantId,
                tenantName: request.tenant.name
            };
        }
        return null;
    }
}
exports.TenantEnricher = TenantEnricher;
//# sourceMappingURL=TenantEnricher.js.map