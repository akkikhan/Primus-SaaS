/**
 * Base interface for enrichers
 */
export interface Enricher {
    enrich(context: Record<string, any>, request?: any): Record<string, any>;
}
/**
 * Enricher that adds request-specific context
 * Auto-detects web frameworks (Express, Fastify, etc.)
 */
export declare class RequestEnricher implements Enricher {
    enrich(context: Record<string, any>, request?: any): Record<string, any>;
    /**
     * Check if this is a web request (Express, Fastify, etc.)
     */
    private isWebRequest;
    /**
     * Get existing request ID or generate a new one
     */
    private getOrGenerateRequestId;
    /**
     * Generate a unique request ID
     */
    private generateRequestId;
}
//# sourceMappingURL=RequestEnricher.d.ts.map