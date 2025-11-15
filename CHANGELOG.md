# Changelog

All notable changes to the Primus SaaS Identity Validator SDKs will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-15

### Added - .NET SDK

#### Core Features
- JWT Bearer authentication middleware for ASP.NET Core applications
- `AddPrimusIdentity()` extension method for simple integration
- `PrimusIdentityOptions` configuration with validation
- `PrimusUser` model for user information extraction
- `GetPrimusUser()` HttpContext extension for easy user access
- Automatic defaults for Issuer and Audience from PortalUrl and ClientId
- Comprehensive XML documentation for IntelliSense support

#### Configuration
- Fluent API configuration with `Action<PrimusIdentityOptions>`
- IValidateOptions implementation for startup validation
- Support for custom Issuer, Audience, and token validation parameters
- Environment-based configuration via appsettings.json

#### Testing
- 18 comprehensive unit tests with xUnit
- 100% test pass rate
- Tests for configuration validation, user extraction, and HttpContext extensions
- Mock-based testing with Moq and FluentAssertions

#### Package
- NuGet package: PrimusSaaS.Identity.Validator v1.0.0
- Symbol package (.snupkg) for debugging support
- README.md included in package
- Dependencies: Microsoft.AspNetCore.Authentication.JwtBearer 7.0.20

### Added - Node.js SDK

#### Core Features
- Express.js middleware for JWT authentication
- `primusIdentityMiddleware()` function for token validation
- `requireRoles()` middleware for role-based access control (RBAC)
- `validateToken()` and `extractUser()` utility functions
- TypeScript support with full type declarations
- Comprehensive error handling (401, 403 status codes)

#### Configuration
- Simple configuration object with required fields validation
- Automatic defaults for issuer, audience, and validation options
- Support for clock skew, HTTPS metadata validation
- Environment variable integration

#### Testing
- 25 comprehensive unit tests with Jest
- 100% test pass rate
- Tests for validation, token verification, middleware, and RBAC
- Mock-based testing with jest.fn()

#### Package
- npm package: @primus-saas/identity-validator v1.0.0
- TypeScript declaration files (.d.ts)
- Source maps for debugging
- Peer dependency: express ^4.21.2
- Dependency: jsonwebtoken ^9.0.2

### Added - Examples

#### .NET Example (examples/dotnet-api/)
- ASP.NET Core Web API demonstrating SDK usage
- Public and protected endpoints
- Role-based authorization examples
- WeatherController with authenticated endpoints
- Comprehensive README with setup and testing guide
- Configuration examples for development and production

#### Node.js Example (examples/nodejs-express/)
- Express.js TypeScript application
- 6 example endpoints (public, protected, admin, management, weather)
- Role-based access control demonstrations
- Request logging with Morgan
- Environment variable configuration
- Comprehensive README with API documentation

### Added - Documentation

#### SDK Documentation
- Detailed README files for both .NET and Node.js SDKs
- Installation instructions
- Quick start guides
- API reference documentation
- Configuration options
- Error handling patterns
- Testing guidelines
- Troubleshooting sections

#### Example Documentation
- Complete setup instructions
- API endpoint documentation
- JWT token testing guides
- Code walkthroughs
- TypeScript usage examples
- Troubleshooting guides

### Technical Details

#### .NET SDK
- Target Framework: .NET 7.0
- Package Size: 10,999 bytes (.nupkg) + 13,837 bytes (.snupkg)
- Test Coverage: 18 tests, 100% passing
- Build Configuration: Release mode with symbol packages

#### Node.js SDK
- Runtime: Node.js 16+
- TypeScript Version: 5.7.3
- Package Size: ~10KB compiled output
- Test Coverage: 25 tests, 100% passing
- Module Format: CommonJS with ES2020 target

### Dependencies

#### .NET SDK Dependencies
- Microsoft.AspNetCore.Authentication.JwtBearer 7.0.20
- Microsoft.Extensions.Options 10.0.0
- System.IdentityModel.Tokens.Jwt 8.14.0

#### Node.js SDK Dependencies
- Production: jsonwebtoken ^9.0.2
- Peer: express ^4.21.2
- Development: TypeScript, Jest, ESLint, Prettier

## [Unreleased]

### Planned Features
- NestJS module for Node.js SDK
- Refresh token handling
- Token revocation support
- Additional authentication providers
- Enhanced logging capabilities
- Performance optimizations
- Additional example projects (React, Angular, Blazor)

---

## Release Notes

### Version 1.0.0 Highlights

This is the initial release of the Primus SaaS Identity Validator SDKs, providing production-ready JWT authentication for both .NET and Node.js applications.

**Key Achievements:**
- ✅ 43 total tests across both platforms (100% passing)
- ✅ Consistent API design between .NET and Node.js
- ✅ Comprehensive documentation and examples
- ✅ TypeScript support with full type safety
- ✅ Symbol packages and source maps for debugging
- ✅ Production-ready packages for NuGet.org and npm

**Getting Started:**
- .NET: `dotnet add package PrimusSaaS.Identity.Validator`
- Node.js: `npm install @primus-saas/identity-validator`

For more information, see the README files in each SDK directory and the example projects.

---

[1.0.0]: https://github.com/akkikhan/Primus-SaaS/releases/tag/v1.0.0
