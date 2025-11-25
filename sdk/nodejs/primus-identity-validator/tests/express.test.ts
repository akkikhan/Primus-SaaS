import { Request, Response } from 'express';
import { sign } from 'jsonwebtoken';
import { primusIdentityMiddleware, requireRoles } from '../src/express';
import { PrimusIdentityOptions, IssuerConfig, IssuerType } from '../src/types';

/** Helper to build options with a single local JWT issuer */
function buildOptions(): PrimusIdentityOptions {
  const localIssuer: IssuerConfig = {
    name: 'LocalAuth',
    type: 'jwt' as IssuerType,
    issuer: 'https://auth.local',
    secret: 'test-jwt-secret-key',
    audiences: ['test-client']
  };
  return {
    issuers: [localIssuer],
    clockSkew: 300,
    validateLifetime: true,
    jwksCacheTtl: 24
  };
}

describe('primusIdentityMiddleware', () => {
  const options = buildOptions();

  const createMockRequest = (authHeader?: string, ip: string = '127.0.0.1'): Partial<Request> => ({
    headers: authHeader ? { authorization: authHeader } : {},
    ip
  });

  const createMockResponse = (): Partial<Response> => {
    const res: Partial<Response> = {};
    res.status = jest.fn().mockReturnValue(res);
    res.json = jest.fn().mockReturnValue(res);
    res.setHeader = jest.fn();
    return res;
  };

  const nextFunction = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should attach user to request with valid token', async () => {
    const payload = {
      sub: 'user-123',
      email: 'test@example.com',
      name: 'Test User',
      role: 'Admin',
      iss: 'https://auth.local',
      aud: 'test-client'
    };
    const token = sign(payload, 'test-jwt-secret-key', { expiresIn: '1h' });
    const req = createMockRequest(`Bearer ${token}`) as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    await middleware(req, res, nextFunction);

    expect(req.primusUser).toBeDefined();
    expect(req.primusUser?.userId).toBe('user-123');
    expect(req.primusUser?.email).toBe('test@example.com');
    expect(nextFunction).toHaveBeenCalled();
  });

  it('should return 401 when Authorization header is missing', async () => {
    const req = createMockRequest() as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    await middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalled();
    expect(nextFunction).not.toHaveBeenCalled();
  });

  it('should return 401 when Authorization header format is invalid', async () => {
    const req = createMockRequest('InvalidFormat token') as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    await middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({
      error: 'Invalid Authorization header format. Expected: Bearer <token>'
    });
    expect(nextFunction).not.toHaveBeenCalled();
  });

  it('should return 401 when token is missing', async () => {
    const req = createMockRequest('Bearer ') as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    await middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({ error: 'Missing token in Authorization header' });
    expect(nextFunction).not.toHaveBeenCalled();
  });

  it('should return 401 when token is invalid', async () => {
    const req = createMockRequest('Bearer invalid-token') as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    await middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalled();
    expect(nextFunction).not.toHaveBeenCalled();
  });

  it('should attach tenant context to primusTenantContext when resolver provided', async () => {
    const payload = {
      sub: 'user-123',
      email: 'test@example.com',
      name: 'Test User',
      role: 'Admin',
      iss: 'https://auth.local',
      aud: 'test-client'
    };

    const token = sign(payload, 'test-jwt-secret-key', { expiresIn: '1h' });
    const req = createMockRequest(`Bearer ${token}`) as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware({
      ...options,
      tenantResolver: claims => ({
        tenantId: (claims['iss'] as string) || 'default',
        roles: ['Admin']
      })
    });

    await middleware(req, res, nextFunction);

    expect((req as any).primusTenantContext).toBeDefined();
    expect((req as any).primusTenantContext?.tenantId).toBe('https://auth.local');
    expect(nextFunction).toHaveBeenCalled();
  });

  it('should rate limit repeated failed requests when enabled', async () => {
    const rateLimitedMiddleware = primusIdentityMiddleware({
      ...options,
      rateLimiting: {
        enabled: true,
        maxFailuresPerWindow: 1,
        windowSeconds: 60
      }
    });

    const req1 = createMockRequest('Bearer invalid-token') as Request;
    const res1 = createMockResponse() as Response;
    await rateLimitedMiddleware(req1, res1, nextFunction);
    expect(res1.status).toHaveBeenCalledWith(401);

    const req2 = createMockRequest('Bearer another-invalid-token') as Request;
    const res2 = createMockResponse() as Response;
    await rateLimitedMiddleware(req2, res2, nextFunction);

    expect(res2.status).toHaveBeenCalledWith(429);
    expect(res2.setHeader).toHaveBeenCalledWith('Retry-After', expect.any(String));
    expect(res2.json).toHaveBeenCalledWith(
      expect.objectContaining({ error: expect.stringContaining('Too many failed authentication attempts') })
    );
  });
});

describe('requireRoles', () => {
  const createMockRequest = (roles?: string[]): Partial<Request> => ({
    primusUser: roles
      ? {
        userId: 'user-123',
        email: 'test@example.com',
        name: 'Test User',
        roles,
        additionalClaims: {}
      }
      : undefined
  });

  const createMockResponse = (): Partial<Response> => {
    const res: Partial<Response> = {};
    res.status = jest.fn().mockReturnValue(res);
    res.json = jest.fn().mockReturnValue(res);
    return res;
  };

  const nextFunction = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should call next when user has required role', () => {
    const req = createMockRequest(['Admin', 'User']) as Request;
    const res = createMockResponse() as Response;

    const middleware = requireRoles('Admin');
    middleware(req, res, nextFunction);

    expect(nextFunction).toHaveBeenCalled();
  });

  it('should call next when user has one of multiple required roles', () => {
    const req = createMockRequest(['User']) as Request;
    const res = createMockResponse() as Response;

    const middleware = requireRoles('Admin', 'User');
    middleware(req, res, nextFunction);

    expect(nextFunction).toHaveBeenCalled();
  });

  it('should return 403 when user lacks required role', () => {
    const req = createMockRequest(['User']) as Request;
    const res = createMockResponse() as Response;

    const middleware = requireRoles('Admin');
    middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(403);
    expect(res.json).toHaveBeenCalledWith({
      error: 'Insufficient permissions',
      requiredRoles: ['Admin']
    });
    expect(nextFunction).not.toHaveBeenCalled();
  });

  it('should return 401 when user is not authenticated', () => {
    const req = createMockRequest() as Request;
    const res = createMockResponse() as Response;

    const middleware = requireRoles('Admin');
    middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({ error: 'User not authenticated' });
    expect(nextFunction).not.toHaveBeenCalled();
  });
});
