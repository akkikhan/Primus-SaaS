# Changelog

All notable changes to the Primus Identity Validator for Node.js will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.2] - 2025-11-25

### Added
- Rate limiting for repeated failed authentications (429 with `Retry-After`) mirroring NuGet defaults.
- HTTPS enforcement toggle (`requireHttpsMetadata`) for OIDC metadata/JWKS endpoints.
- Tenant resolver context now exposed as `req.primusTenantContext` for downstream logging middleware.
- New documentation: Tenant Resolver Guide, Testing Guide, Secret Management, Claims Mapping, Angular Integration, Diagnostics/Troubleshooting.

### Changed
- Defaults now set for `validateLifetime` and `requireHttpsMetadata`.
- Express middleware aligns error responses and tenant context names with .NET implementation.

### Fixed
- Added IP-aware keying for rate limiter to prevent global throttling collisions.
- Improved validation errors for missing authority over plain HTTP.

## [1.0.0] - 2025-01-XX

### Added
- Azure AD token validation with RS256 signature verification
- `ValidationMode` enum to support Local, AzureAd, and Hybrid validation modes
- `AzureAdValidator` class for validating Azure AD JWT tokens
- `OpenIdConfigurationService` for fetching and caching Azure AD OpenID Connect configuration
- `JwksService` for fetching and caching JSON Web Key Sets (JWKS) from Azure AD
- `JwksCache` utility for JWK-to-PEM conversion and caching
- Support for multiple Azure AD issuer formats (v1.0, v2.0, sts.windows.net)
- Hybrid mode with automatic fallback between Azure AD and Local validation
- `tenantId` configuration option for Azure AD tenant validation
- `jwksCacheTtl` option to control JWKS cache expiration (default: 24 hours)
- Comprehensive test suite with 83 tests and 99.18% code coverage
- TypeScript type definitions for all Azure AD components
- Express middleware (`primusIdentityMiddleware`) with Azure AD support
- Role-based authorization middleware (`requireRoles`)

### Changed
- `PrimusIdentityValidator` now supports three validation modes: Local, AzureAd, Hybrid
- Configuration options expanded to include Azure AD-specific parameters
- Enhanced error messages for Azure AD validation failures

### Fixed
- Clock skew tolerance now properly applied to Azure AD token validation
- Improved error handling for JWKS fetch failures
- Thread-safe JWKS caching with concurrent fetch prevention

## [0.1.0] - Previous Release

### Added
- Initial release with Local JWT validation using HMAC (HS256)
- Token signature verification
- Expiration time validation
- Token audience validation
- Express middleware integration
- Role-based authorization
- TypeScript support
