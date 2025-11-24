process.env.API_AUDIENCE = 'TEST-APP-ID';
process.env.LOCAL_SECRET = 'test-local-secret-1234567890';
process.env.LOCAL_ISSUER = 'http://localhost:4000';
process.env.NODE_ENV = 'test';

const jwt = require('jsonwebtoken');
const request = require('supertest');
const { app, logger } = require('../server');

function signLocalToken(overrides = {}) {
    const payload = {
        sub: overrides.sub || 'user-123',
        email: overrides.email || 'user@example.com',
        name: overrides.name || 'Test User',
        role: overrides.role || ['User']
    };

    return jwt.sign(payload, process.env.LOCAL_SECRET, {
        algorithm: 'HS256',
        expiresIn: overrides.expiresIn || '1h',
        issuer: process.env.LOCAL_ISSUER,
        audience: process.env.API_AUDIENCE
    });
}

describe('Claims API end-to-end (local issuer)', () => {
    it('exposes a public health endpoint', async () => {
        const res = await request(app).get('/api/health');
        expect(res.status).toBe(200);
        expect(res.body.status).toBe('healthy');
    });

    it('rejects protected endpoint without a token', async () => {
        const res = await request(app).get('/api/revenue-stats');
        expect(res.status).toBe(401);
        expect(res.body.error).toBeDefined();
    });

    it('allows access with a valid local JWT (happy path)', async () => {
        const token = signLocalToken({ sub: 'adjuster-1', role: ['Adjuster'] });
        const spy = jest.spyOn(logger, 'info');

        const res = await request(app)
            .get('/api/revenue-stats')
            .set('Authorization', `Bearer ${token}`);

        expect(res.status).toBe(200);
        expect(res.body.company).toBe('Acme Corp');
        expect(res.body.user).toMatchObject({
            userId: 'adjuster-1',
            email: 'user@example.com',
            roles: ['Adjuster']
        });

        expect(spy).toHaveBeenCalledWith(
            'Revenue stats requested',
            expect.objectContaining({ path: '/api/revenue-stats' })
        );
        spy.mockRestore();
    });

    it('rejects tokens with the wrong audience', async () => {
        const badToken = jwt.sign(
            {
                sub: 'user-999',
                email: 'user@example.com'
            },
            process.env.LOCAL_SECRET,
            {
                algorithm: 'HS256',
                expiresIn: '1h',
                issuer: process.env.LOCAL_ISSUER,
                audience: 'WRONG-AUD'
            }
        );

        const res = await request(app)
            .get('/api/revenue-stats')
            .set('Authorization', `Bearer ${badToken}`);

        expect(res.status).toBe(401);
        expect(res.body.error).toMatch(/audience/i);
    });
});
