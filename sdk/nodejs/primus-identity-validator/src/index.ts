// Export types
export type {
  PrimusIdentityOptions,
  IssuerConfig,
  IssuerType,
  PrimusUser,
  JwtPayload,
  TokenValidationResult,
  TenantContext,
  OpenIdConfiguration,
  JsonWebKeySet,
  JsonWebKey
} from './types';

// Export main validator
export { PrimusIdentityValidator } from './validator';

// Export Express middleware
export { primusIdentityMiddleware, requireRoles } from './express';

// Alias for convenience (matches spec)
export { primusIdentityMiddleware as primusIdentityValidator } from './express';

// Export validators for advanced usage
export { LocalValidator } from './validators/localValidator';
export { AzureAdValidator } from './validators/azureAdValidator';

// Export services for advanced usage
export { OpenIdConfigurationService } from './services/openIdConfigurationService';
export { JwksService } from './services/jwksService';
export { JwksCache } from './services/jwksCache';
