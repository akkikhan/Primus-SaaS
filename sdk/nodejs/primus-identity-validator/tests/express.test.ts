import { Request, Response } from 'express';
import { sign } from 'jsonwebtoken';
import { primusIdentityMiddleware, requireRoles } from '../src/express';
import { PrimusIdentityOptions } from '../src/types';

describe('primusIdentityMiddleware', () => {
  const options: PrimusIdentityOptions = {
    portalUrl: 'https://portal.primus-saas.com',
    clientId: 'test-client',
    clientSecret: 'test-secret',
    jwtSecret: 'test-jwt-secret-key',
  };

  const createMockRequest = (authHeader?: string): Partial<Request> => ({
    headers: authHeader ? { authorization: authHeader } : {},
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

  it('should attach user to request with valid token', () => {
    const payload = {
      sub: 'user-123',
      email: 'test@example.com',
      name: 'Test User',
      role: 'Admin',
      iss: 'https://portal.primus-saas.com',
      aud: 'test-client',
    };

    const token = sign(payload, options.jwtSecret, { expiresIn: '1h' });
    const req = createMockRequest(`Bearer ${token}`) as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    middleware(req, res, nextFunction);

    expect(req.primusUser).toBeDefined();
    expect(req.primusUser?.userId).toBe('user-123');
    expect(req.primusUser?.email).toBe('test@example.com');
    expect(nextFunction).toHaveBeenCalled();
  });

  it('should return 401 when Authorization header is missing', () => {
    const req = createMockRequest() as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({ error: 'Missing Authorization header' });
    expect(nextFunction).not.toHaveBeenCalled();
  });

  it('should return 401 when Authorization header format is invalid', () => {
    const req = createMockRequest('InvalidFormat token') as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({
      error: 'Invalid Authorization header format. Expected: Bearer <token>',
    });
    expect(nextFunction).not.toHaveBeenCalled();
  });

  it('should return 401 when token is missing', () => {
    const req = createMockRequest('Bearer ') as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalledWith({ error: 'Missing token in Authorization header' });
    expect(nextFunction).not.toHaveBeenCalled();
  });

  it('should return 401 when token is invalid', () => {
    const req = createMockRequest('Bearer invalid-token') as Request;
    const res = createMockResponse() as Response;

    const middleware = primusIdentityMiddleware(options);
    middleware(req, res, nextFunction);

    expect(res.status).toHaveBeenCalledWith(401);
    expect(res.json).toHaveBeenCalled();
    expect(nextFunction).not.toHaveBeenCalled();
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
          additionalClaims: {},
        }
      : undefined,
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
      requiredRoles: ['Admin'],
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
