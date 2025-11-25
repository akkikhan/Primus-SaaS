import { Enricher, RequestEnricher } from '../enrichers/RequestEnricher';
import { UserEnricher } from '../enrichers/UserEnricher';
import { TenantEnricher } from '../enrichers/TenantEnricher';

/**
 * Context manager that applies enrichers
 */
export class Context {
    private enrichers: Enricher[];

    constructor(additionalEnrichers: Enricher[] = []) {
        // Register default enrichers
        this.enrichers = [
            new RequestEnricher(),
            new UserEnricher(),
            new TenantEnricher(),
            ...additionalEnrichers
        ];
    }

    /**
     * Enrich context with all registered enrichers
     */
    enrich(baseContext: Record<string, any>, request?: any): Record<string, any> {
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
    addEnricher(enricher: Enricher): void {
        this.enrichers.push(enricher);
    }
}
