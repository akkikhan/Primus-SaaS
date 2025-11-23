"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Context = void 0;
const RequestEnricher_1 = require("../enrichers/RequestEnricher");
const UserEnricher_1 = require("../enrichers/UserEnricher");
const TenantEnricher_1 = require("../enrichers/TenantEnricher");
/**
 * Context manager that applies enrichers
 */
class Context {
    constructor() {
        // Register default enrichers
        this.enrichers = [
            new RequestEnricher_1.RequestEnricher(),
            new UserEnricher_1.UserEnricher(),
            new TenantEnricher_1.TenantEnricher()
        ];
    }
    /**
     * Enrich context with all registered enrichers
     */
    enrich(baseContext, request) {
        let enrichedContext = { ...baseContext };
        // Apply each enricher
        for (const enricher of this.enrichers) {
            enrichedContext = enricher.enrich(enrichedContext, request);
        }
        return enrichedContext;
    }
    /**
     * Add a custom enricher
     */
    addEnricher(enricher) {
        this.enrichers.push(enricher);
    }
}
exports.Context = Context;
//# sourceMappingURL=Context.js.map