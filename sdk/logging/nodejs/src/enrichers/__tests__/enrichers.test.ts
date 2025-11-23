import { RequestEnricher } from '../RequestEnricher';
import { UserEnricher } from '../UserEnricher';
import { TenantEnricher } from '../TenantEnricher';

describe('Enrichers', () => {
    describe('RequestEnricher', () => {
        const enricher = new RequestEnricher();

        it('should add requestId for Express requests', () => {
            const mockRequest = {
                method: 'GET',
                url: '/api/users',
                headers: {}
            };

            const result = enricher.enrich({}, mockRequest);

            expect(result.requestId).toBeDefined();
            expect(result.requestId).toMatch(/^req-/);
        });

        it('should use existing X-Request-ID header', () => {
            const mockRequest = {
                method: 'GET',
                url: '/api/users',
                headers: {
                    'x-request-id': 'existing-req-123'
                }
            };

            const result = enricher.enrich({}, mockRequest);

            expect(result.requestId).toBe('existing-req-123');
        });

        it('should not add requestId for non-web requests', () => {
            const result = enricher.enrich({}, null);

            expect(result.requestId).toBeUndefined();
        });
    });

    describe('UserEnricher', () => {
        const enricher = new UserEnricher();

        it('should extract user context from Primus Identity Validator', () => {
            const mockRequest = {
                primusUser: {
                    userId: '12345',
                    email: 'john@example.com',
                    roles: ['user', 'admin']
                }
            };

            const result = enricher.enrich({}, mockRequest);

            expect(result.userId).toBe('12345');
            expect(result.userEmail).toBe('john@example.com');
            expect(result.userRoles).toEqual(['user', 'admin']);
        });

        it('should extract user context from Passport.js', () => {
            const mockRequest = {
                user: {
                    id: '67890',
                    email: 'jane@example.com',
                    roles: ['user']
                }
            };

            const result = enricher.enrich({}, mockRequest);

            expect(result.userId).toBe('67890');
            expect(result.userEmail).toBe('jane@example.com');
            expect(result.userRoles).toEqual(['user']);
        });

        it('should not add user context when no user is present', () => {
            const mockRequest = {};

            const result = enricher.enrich({}, mockRequest);

            expect(result.userId).toBeUndefined();
            expect(result.userEmail).toBeUndefined();
        });
    });

    describe('TenantEnricher', () => {
        const enricher = new TenantEnricher();

        it('should extract tenant context from Primus Identity Validator', () => {
            const mockRequest = {
                primusTenantContext: {
                    tenantId: 'acme-corp',
                    tenantName: 'Acme Corporation'
                }
            };

            const result = enricher.enrich({}, mockRequest);

            expect(result.tenantId).toBe('acme-corp');
            expect(result.tenantName).toBe('Acme Corporation');
        });

        it('should extract tenant from X-Tenant-ID header', () => {
            const mockRequest = {
                headers: {
                    'x-tenant-id': 'tenant-123'
                }
            };

            const result = enricher.enrich({}, mockRequest);

            expect(result.tenantId).toBe('tenant-123');
        });

        it('should not add tenant context when no tenant is present', () => {
            const mockRequest = {};

            const result = enricher.enrich({}, mockRequest);

            expect(result.tenantId).toBeUndefined();
            expect(result.tenantName).toBeUndefined();
        });
    });
});
