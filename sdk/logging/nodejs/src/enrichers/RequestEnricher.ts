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
export class RequestEnricher implements Enricher {
    enrich(context: Record<string, any>, request?: any): Record<string, any> {
        if (!request) {
            return context;
        }

        // Auto-detect Express/Fastify request
        if (this.isWebRequest(request)) {
            return {
                ...context,
                requestId: this.getOrGenerateRequestId(request)
            };
        }

        return context;
    }

    /**
     * Check if this is a web request (Express, Fastify, etc.)
     */
    private isWebRequest(request: any): boolean {
        // Express: has req.method, req.url, req.headers
        // Fastify: has request.method, request.url, request.headers
        return !!(
            request &&
            (request.method || request.req?.method) &&
            (request.url || request.req?.url) &&
            (request.headers || request.req?.headers)
        );
    }

    /**
     * Get existing request ID or generate a new one
     */
    private getOrGenerateRequestId(request: any): string {
        // Check common request ID headers
        const headers = request.headers || request.req?.headers || {};

        // Common request ID headers
        const requestId =
            headers['x-request-id'] ||
            headers['x-correlation-id'] ||
            headers['x-amzn-trace-id'] ||
            this.generateRequestId();

        return requestId;
    }

    /**
     * Generate a unique request ID
     */
    private generateRequestId(): string {
        return `req-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }
}
