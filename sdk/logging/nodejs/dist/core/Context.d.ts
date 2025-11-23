import { Enricher } from '../enrichers/RequestEnricher';
/**
 * Context manager that applies enrichers
 */
export declare class Context {
    private enrichers;
    constructor();
    /**
     * Enrich context with all registered enrichers
     */
    enrich(baseContext: Record<string, any>, request?: any): Record<string, any>;
    /**
     * Add a custom enricher
     */
    addEnricher(enricher: Enricher): void;
}
//# sourceMappingURL=Context.d.ts.map