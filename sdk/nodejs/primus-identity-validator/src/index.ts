// Export types
export { PrimusIdentityOptions, PrimusUser, JwtPayload } from './types';

// Export validator utilities
export { validateOptions, applyDefaults, validateToken, extractUser } from './validator';

// Export Express middleware
export { primusIdentityMiddleware, requireRoles } from './express';
