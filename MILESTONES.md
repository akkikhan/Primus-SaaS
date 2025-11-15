# Primus SaaS Platform - Milestones & Roadmap

**Version**: 1.0  
**Date**: November 15, 2025  
**Scope**: Excludes Usage & Billing, Observability Integration

---

## Overview

This document defines actionable milestones to address gaps identified in `GAP_ANALYSIS.md`. Each milestone includes specific tasks, acceptance criteria, and estimated timelines.

**Excluded from Scope** (as per business decision):
- ❌ Usage & Billing (Section 3.1)
- ❌ Observability Integration (Section 3.2)
- ❌ SDK Telemetry to Portal (Section 7.2 - depends on observability)

---

## Milestone 1: Azure AD Core Validation (MVP Blocker)
**Priority**: P0 - Critical  
**Timeline**: 3-4 weeks  
**Goal**: Make Azure AD authentication mode functional

### Tasks

#### 1.1 Azure AD Token Validation - .NET SDK
- [ ] Implement JWKS fetching from `https://login.microsoftonline.com/{tenant}/discovery/v2.0/keys`
- [ ] Implement OpenID Connect metadata discovery (`.well-known/openid-configuration`)
- [ ] Add signature verification using Azure AD public keys (RS256)
- [ ] Add issuer validation (`https://login.microsoftonline.com/{tenant}/v2.0`)
- [ ] Add audience validation (client ID from configuration)
- [ ] Add algorithm enforcement (reject tokens not using RS256)
- [ ] Implement in-memory JWKS caching with TTL (24 hours default)
- [ ] Add configuration options for cache TTL and JWKS endpoint override

**Acceptance Criteria**:
- ✅ SDK validates real Azure AD tokens successfully
- ✅ Invalid signatures are rejected
- ✅ Expired tokens are rejected
- ✅ Wrong audience/issuer tokens are rejected
- ✅ JWKS cache reduces latency to <5ms for cached keys

**Files to Modify**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator/AzureAdValidator.cs` (new)
- `sdk/dotnet/PrimusSaaS.Identity.Validator/JwksCache.cs` (new)
- `sdk/dotnet/PrimusSaaS.Identity.Validator/IdentityValidatorMiddleware.cs`
- `sdk/dotnet/PrimusSaaS.Identity.Validator/Configuration/IdentityValidatorOptions.cs`

---

#### 1.2 Azure AD Token Validation - Node.js SDK
- [ ] Implement JWKS fetching using `node-fetch` or `axios`
- [ ] Implement OpenID Connect metadata discovery
- [ ] Add signature verification using `jsonwebtoken` with JWKS
- [ ] Add issuer validation
- [ ] Add audience validation
- [ ] Add algorithm enforcement
- [ ] Implement in-memory JWKS caching with TTL
- [ ] Add TypeScript type definitions for all validation types

**Acceptance Criteria**:
- ✅ SDK validates real Azure AD tokens successfully
- ✅ All validation rules enforced (signature, issuer, audience, expiry, algorithm)
- ✅ JWKS cache reduces latency to <5ms for cached keys
- ✅ TypeScript typings are complete and accurate

**Files to Modify**:
- `sdk/nodejs/primus-identity-validator/src/validators/azureAdValidator.ts` (new)
- `sdk/nodejs/primus-identity-validator/src/cache/jwksCache.ts` (new)
- `sdk/nodejs/primus-identity-validator/src/middleware/identityMiddleware.ts`
- `sdk/nodejs/primus-identity-validator/src/types/index.ts`

---

#### 1.3 Azure AD Integration Tests
- [ ] Create mock JWKS endpoint for testing (.NET)
- [ ] Create mock JWKS endpoint for testing (Node.js)
- [ ] Write tests for successful validation
- [ ] Write tests for invalid signature
- [ ] Write tests for expired tokens
- [ ] Write tests for wrong issuer
- [ ] Write tests for wrong audience
- [ ] Write tests for unsupported algorithm (HS256, none)
- [ ] Write tests for JWKS cache hit/miss
- [ ] Write tests for JWKS cache expiry and refresh

**Acceptance Criteria**:
- ✅ Test coverage >90% for Azure AD validation code
- ✅ All edge cases covered (expired keys, network failures, malformed tokens)
- ✅ Tests run in <30 seconds

**Files to Create**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator.Tests/AzureAdValidatorTests.cs`
- `sdk/dotnet/PrimusSaaS.Identity.Validator.Tests/JwksCacheTests.cs`
- `sdk/nodejs/primus-identity-validator/src/__tests__/azureAdValidator.test.ts`
- `sdk/nodejs/primus-identity-validator/src/__tests__/jwksCache.test.ts`

---

## Milestone 2: Multi-Tenant Architecture Support
**Priority**: P0 - Critical  
**Timeline**: 2-3 weeks  
**Goal**: Enable tenant isolation and secure multi-tenant deployments

### Tasks

#### 2.1 Portal - Tenant Configuration Storage
- [ ] Add `TenantId` field to `Application` model (Azure AD Tenant ID)
- [ ] Add `Environment` field to `Application` model (Dev/Test/Prod)
- [ ] Create database migration for new fields
- [ ] Update ApplicationsController to accept tenant ID during registration
- [ ] Update ApplicationDetailsDto to include tenant ID
- [ ] Add validation: Tenant ID must be valid GUID format
- [ ] Update UI to show tenant ID input field on application creation
- [ ] Update auto-generated documentation to include actual tenant ID (not placeholder)

**Acceptance Criteria**:
- ✅ Portal stores Azure AD Tenant ID per application
- ✅ Applications can have multiple configurations (one per environment)
- ✅ Documentation shows actual tenant ID, not `<YOUR_TENANT_ID>`

**Files to Modify**:
- `portal/backend/Models/Application.cs`
- `portal/backend/Controllers/ApplicationsController.cs`
- `portal/backend/Data/PortalDbContext.cs`
- `portal/backend/Migrations/` (new migration file)
- `portal/frontend/src/pages/ApplicationDetailsPage.tsx`

---

#### 2.2 SDK - Tenant Validation
- [ ] Add `TenantId` to configuration (required when Mode=AzureAd)
- [ ] Extract tenant ID from token issuer claim (`iss`)
- [ ] Compare extracted tenant ID with configured tenant ID
- [ ] Reject tokens if tenant mismatch
- [ ] Add custom claim `PrimusTenantId` to ClaimsPrincipal for downstream use
- [ ] Log tenant validation success/failure
- [ ] Implement for .NET SDK
- [ ] Implement for Node.js SDK

**Acceptance Criteria**:
- ✅ SDK rejects tokens from wrong Azure AD tenant
- ✅ Application code can access `PrimusTenantId` from claims
- ✅ Clear error message when tenant mismatch occurs

**Files to Modify**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator/AzureAdValidator.cs`
- `sdk/dotnet/PrimusSaaS.Identity.Validator/Configuration/IdentityValidatorOptions.cs`
- `sdk/nodejs/primus-identity-validator/src/validators/azureAdValidator.ts`
- `sdk/nodejs/primus-identity-validator/src/types/index.ts`

---

#### 2.3 Multi-Environment Support
- [ ] Update Portal to support multiple configs per application (e.g., Dev/Test/Prod)
- [ ] Add `ApplicationEnvironment` table (ApplicationId, Environment, TenantId, ClientId)
- [ ] Update UI to show tabs for each environment
- [ ] Generate separate documentation per environment
- [ ] Add environment selector in SDK configuration examples

**Acceptance Criteria**:
- ✅ Applications can have separate Azure AD tenants for Dev/Test/Prod
- ✅ Documentation clearly shows environment-specific configuration

**Files to Create**:
- `portal/backend/Models/ApplicationEnvironment.cs`
- `portal/backend/Migrations/` (new migration)

**Files to Modify**:
- `portal/frontend/src/pages/ApplicationDetailsPage.tsx`
- `portal/backend/Controllers/ApplicationsController.cs`

---

## Milestone 3: Security Hardening
**Priority**: P0 - Critical  
**Timeline**: 2 weeks  
**Goal**: Ensure production-grade security

### Tasks

#### 3.1 JWT Security Testing
- [ ] Test for `alg: none` vulnerability (unsigned tokens)
- [ ] Test for weak algorithms (HS256 with known secret)
- [ ] Test for signature bypass (modify payload without re-signing)
- [ ] Test for key confusion attacks (using public key as HMAC secret)
- [ ] Test for expired token acceptance
- [ ] Test for token reuse after logout (if revocation implemented)
- [ ] Document all tested attack vectors

**Acceptance Criteria**:
- ✅ SDK rejects all tested attack vectors
- ✅ Security test suite documented in `sdk/SECURITY_TESTS.md`

**Files to Create**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator.Tests/SecurityTests.cs`
- `sdk/nodejs/primus-identity-validator/src/__tests__/security.test.ts`
- `sdk/SECURITY_TESTS.md`

---

#### 3.2 Production Database Setup
- [ ] Provision Azure SQL Database (Standard tier)
- [ ] Configure firewall rules for Azure App Service
- [ ] Enable automated backups (point-in-time restore)
- [ ] Create database user for portal application (least privilege)
- [ ] Store connection string in Azure Key Vault
- [ ] Update portal `appsettings.Production.json` to read from Key Vault
- [ ] Test connection from deployed portal

**Acceptance Criteria**:
- ✅ Production database running on Azure SQL
- ✅ Connection string stored securely in Key Vault
- ✅ Automated backups enabled

**Files to Modify**:
- `portal/backend/appsettings.Production.json`
- `portal/backend/Program.cs` (add Key Vault configuration)

---

#### 3.3 Secrets Management (Key Vault)
- [ ] Create Azure Key Vault instance
- [ ] Grant portal Managed Identity access to Key Vault
- [ ] Move JWT signing keys to Key Vault (local mode)
- [ ] Move database connection strings to Key Vault
- [ ] Update portal to fetch secrets from Key Vault at startup
- [ ] Remove all secrets from appsettings files
- [ ] Document Key Vault setup in deployment guide

**Acceptance Criteria**:
- ✅ No secrets in source code or config files
- ✅ Portal fetches all secrets from Key Vault
- ✅ Deployment guide includes Key Vault setup steps

**Files to Modify**:
- `portal/backend/Program.cs`
- `portal/backend/appsettings.json` (remove secrets)

**Files to Create**:
- `portal/DEPLOYMENT.md`

---

## Milestone 4: Documentation & Developer Experience
**Priority**: P1 - High  
**Timeline**: 2 weeks  
**Goal**: Enable easy integration for client developers

### Tasks

#### 4.1 Architecture Diagrams
- [x] Create `auth-seq-azuread.md` (authentication sequence) - **COMPLETED**
- [x] Create `platform-auth-overview.md` (system architecture) - **COMPLETED**
- [x] Create `module-integration-config.md` (integration guide) - **COMPLETED**
- [ ] Create `devsaas-identity-lifecycle.md` (module lifecycle diagram)
- [ ] Review diagrams with stakeholders

**Acceptance Criteria**:
- ✅ All diagrams use Mermaid format
- ✅ Diagrams cover SPA → Azure AD → Backend flow
- ✅ Stakeholder approval obtained

**Files**:
- `docs/diagrams/auth-seq-azuread.md` ✅
- `docs/diagrams/platform-auth-overview.md` ✅
- `docs/diagrams/module-integration-config.md` ✅
- `docs/diagrams/devsaas-identity-lifecycle.md` (pending)

---

#### 4.2 Full-Stack Integration Example
- [ ] Create React + TypeScript example app
- [ ] Integrate MSAL for Azure AD login
- [ ] Call protected backend API with Bearer token
- [ ] Show role-based UI (Admin vs User)
- [ ] Add error handling (401, 403)
- [ ] Document step-by-step setup
- [ ] Add to `examples/` folder

**Acceptance Criteria**:
- ✅ Example runs end-to-end (login → API call → display data)
- ✅ README.md with setup instructions
- ✅ Demonstrates best practices (token storage, error handling)

**Files to Create**:
- `examples/react-msal-app/` (entire folder structure)
- `examples/react-msal-app/README.md`

---

#### 4.3 Migration Guide Template
- [ ] Create template for breaking changes
- [ ] Include sections: Overview, What Changed, Migration Steps, Code Diff
- [ ] Write sample migration guide (example: v1.0 → v2.0)
- [ ] Integrate template into portal (autogenerate from release notes)

**Acceptance Criteria**:
- ✅ Template clearly shows before/after code
- ✅ Portal can auto-populate template with module-specific changes

**Files to Create**:
- `docs/MIGRATION_GUIDE_TEMPLATE.md`

---

#### 4.4 Troubleshooting Runbook
- [ ] Create flowchart for common auth failures
- [ ] Add section: "Token validation failed" → check signature, expiry, audience
- [ ] Add section: "JWKS fetch failed" → check network, firewall, tenant ID
- [ ] Add section: "Tenant mismatch" → verify tenant ID configuration
- [ ] Add section: "Configuration errors" → validate required fields
- [ ] Link to FAQ and GitHub issues

**Acceptance Criteria**:
- ✅ Covers top 10 common errors
- ✅ Includes copy-paste diagnostic commands
- ✅ Links to relevant documentation sections

**Files to Create**:
- `docs/TROUBLESHOOTING.md`

---

## Milestone 5: Testing & Quality Assurance
**Priority**: P1 - High  
**Timeline**: 2 weeks  
**Goal**: Comprehensive test coverage for production confidence

### Tasks

#### 5.1 E2E Integration Tests
- [ ] Create test: Portal registration → doc generation → SDK integration
- [ ] Create test: Azure AD token flow (SPA → Backend → SDK validation)
- [ ] Create test: Module version update → notification → upgrade
- [ ] Use Playwright or Selenium for portal UI tests
- [ ] Use real Azure AD test tenant for token tests
- [ ] Automate in CI/CD pipeline

**Acceptance Criteria**:
- ✅ E2E tests cover full user journey
- ✅ Tests run in CI/CD on every PR
- ✅ Tests use isolated test data (no prod pollution)

**Files to Create**:
- `test-apps/e2e-tests/` (folder structure)
- `test-apps/e2e-tests/portal-registration.test.ts`
- `test-apps/e2e-tests/azuread-flow.test.ts`

---

#### 5.2 Performance Testing
- [ ] Create load test script (1000 req/sec for 5 minutes)
- [ ] Measure JWKS cache hit rate
- [ ] Measure token validation latency (p50, p95, p99)
- [ ] Identify bottlenecks (DB queries, JWKS fetch, crypto)
- [ ] Optimize based on findings
- [ ] Document performance baselines

**Acceptance Criteria**:
- ✅ Token validation latency p95 < 50ms
- ✅ JWKS cache hit rate > 99%
- ✅ System handles 1000 req/sec without errors

**Files to Create**:
- `test-apps/load-tests/validation-load.js` (using k6 or Artillery)
- `docs/PERFORMANCE_BASELINE.md`

---

## Milestone 6: Deployment & CI/CD
**Priority**: P1 - High  
**Timeline**: 2 weeks  
**Goal**: Automate releases and deployments

### Tasks

#### 6.1 SDK Publishing Pipeline
- [ ] Create GitHub Actions workflow for .NET SDK
- [ ] Workflow steps: Restore → Build → Test → Pack → Publish to NuGet
- [ ] Trigger on git tag (e.g., `v1.0.0`)
- [ ] Add version bump automation (extract from tag)
- [ ] Create GitHub Actions workflow for Node.js SDK
- [ ] Workflow steps: Install → Build → Test → Publish to NPM
- [ ] Add NPM authentication using secrets
- [ ] Test pipeline in dry-run mode

**Acceptance Criteria**:
- ✅ Tagging repo with `v1.0.0` publishes SDK to NuGet/NPM
- ✅ Version numbers match git tag
- ✅ Tests must pass before publish

**Files to Create**:
- `.github/workflows/publish-dotnet-sdk.yml`
- `.github/workflows/publish-nodejs-sdk.yml`

---

#### 6.2 Portal Deployment Pipeline
- [ ] Create GitHub Actions workflow for portal backend
- [ ] Workflow steps: Build → Test → Publish to Azure App Service
- [ ] Create workflow for portal frontend
- [ ] Workflow steps: Build → Deploy to Azure Static Web Apps or Blob Storage
- [ ] Add staging environment deployment (on push to `dev`)
- [ ] Add production environment deployment (on push to `main`)
- [ ] Configure environment-specific secrets in GitHub

**Acceptance Criteria**:
- ✅ Push to `dev` deploys to staging environment
- ✅ Push to `main` deploys to production environment
- ✅ Deployment includes database migrations

**Files to Create**:
- `.github/workflows/deploy-portal-backend.yml`
- `.github/workflows/deploy-portal-frontend.yml`

---

#### 6.3 Infrastructure as Code (Optional Enhancement)
- [ ] Create Bicep/Terraform templates for Azure resources
- [ ] Include: App Service, SQL Database, Key Vault, Static Web App
- [ ] Parameterize by environment (dev/prod)
- [ ] Add to deployment pipeline

**Acceptance Criteria**:
- ✅ Infrastructure can be provisioned with single command
- ✅ Templates version-controlled in repo

**Files to Create**:
- `infrastructure/azure/main.bicep` or `infrastructure/terraform/main.tf`

---

## Milestone 7: Advanced Authentication Features
**Priority**: P2 - Medium  
**Timeline**: 3-4 weeks  
**Goal**: Support advanced enterprise scenarios

### Tasks

#### 7.1 Service-to-Service Authentication
- [ ] Add support for client credentials flow tokens (no user claims)
- [ ] Validate `aud` claim for API identifiers (e.g., `api://my-api`)
- [ ] Validate `roles` or `app_roles` claims for service permissions
- [ ] Add configuration option `AllowServiceTokens`
- [ ] Update documentation with service-to-service examples
- [ ] Implement for .NET SDK
- [ ] Implement for Node.js SDK

**Acceptance Criteria**:
- ✅ SDK validates app-only tokens from Azure AD
- ✅ Service accounts can call APIs without user context
- ✅ Documentation includes setup for Azure AD app registration (service principal)

**Files to Modify**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator/AzureAdValidator.cs`
- `sdk/nodejs/primus-identity-validator/src/validators/azureAdValidator.ts`

**Files to Create**:
- `docs/SERVICE_TO_SERVICE_AUTH.md`

---

#### 7.2 Hybrid Authentication Mode
- [ ] Add support for Mode="Hybrid" (Local + Azure AD simultaneously)
- [ ] Check Authorization header: If starts with "Bearer", try Azure AD validation
- [ ] If Azure AD validation fails, fall back to Local mode
- [ ] Add configuration `PreferredMode` to specify fallback order
- [ ] Log which mode was used for each request
- [ ] Update documentation with hybrid scenarios

**Acceptance Criteria**:
- ✅ SDK accepts both local JWT and Azure AD tokens
- ✅ Fallback logic works correctly
- ✅ Clear logging shows which mode authenticated each request

**Files to Modify**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator/IdentityValidatorMiddleware.cs`
- `sdk/nodejs/primus-identity-validator/src/middleware/identityMiddleware.ts`

---

#### 7.3 Policy-Based Authorization
- [ ] Add policy engine to SDK configuration
- [ ] Define policy format: `{ "name": "AdminOnly", "requires": ["role:Admin", "tenant:12345"] }`
- [ ] Implement policy evaluation logic
- [ ] Add `[Authorize(Policy = "AdminOnly")]` support (.NET)
- [ ] Add equivalent for Node.js (middleware with policy name)
- [ ] Document policy syntax and examples

**Acceptance Criteria**:
- ✅ Policies can combine role, tenant, and custom claims
- ✅ Application code uses policies instead of hard-coded role checks
- ✅ Policies defined in configuration (not code)

**Files to Create**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator/PolicyEngine.cs`
- `sdk/nodejs/primus-identity-validator/src/policies/policyEngine.ts`
- `docs/POLICY_BASED_AUTHORIZATION.md`

---

#### 7.4 Custom Claims Mapping
- [ ] Add configuration to map Azure AD roles/groups to application roles
- [ ] Example: `{ "AzureAdGroup": "Sales-Team", "AppRole": "Salesperson" }`
- [ ] Transform claims during token validation
- [ ] Add transformed claims to ClaimsPrincipal
- [ ] Document mapping configuration

**Acceptance Criteria**:
- ✅ Azure AD group memberships appear as application roles
- ✅ Mapping is configurable per application
- ✅ Works with existing role-based authorization

**Files to Modify**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator/Configuration/IdentityValidatorOptions.cs`
- `sdk/dotnet/PrimusSaaS.Identity.Validator/AzureAdValidator.cs`
- `sdk/nodejs/primus-identity-validator/src/types/index.ts`

---

## Milestone 8: Upgrade Management & Notifications
**Priority**: P2 - Medium  
**Timeline**: 2-3 weeks  
**Goal**: Automate upgrade notifications and streamline version changes

### Tasks

#### 8.1 Email Notification Service
- [ ] Create notification service in portal backend
- [ ] Integrate with SendGrid or Azure Communication Services
- [ ] Add `NotificationPreference` to User model (email opt-in/out)
- [ ] Send email when new module version released
- [ ] Include: Module name, new version, release notes, migration guide link
- [ ] Add "View in Portal" button to email
- [ ] Test email delivery in dev environment

**Acceptance Criteria**:
- ✅ Email sent to application owners when module updates
- ✅ Email includes actionable information (version, changes)
- ✅ Users can opt-out of notifications

**Files to Create**:
- `portal/backend/Services/NotificationService.cs`
- `portal/backend/Templates/module-update-email.html`

**Files to Modify**:
- `portal/backend/Models/User.cs`
- `portal/backend/Controllers/ModulesController.cs` (trigger email on new version)

---

#### 8.2 Upgrade Acknowledgement Workflow
- [ ] Add `UpgradeStatus` field to `ApplicationModule` (Pending, Acknowledged, Completed)
- [ ] Add "Mark as Upgraded" button in portal UI
- [ ] Update status when button clicked
- [ ] Show upgrade history (when upgraded, by whom)
- [ ] Add dashboard showing pending upgrades across all applications

**Acceptance Criteria**:
- ✅ Portal tracks upgrade completion
- ✅ Dashboard shows which apps need attention
- ✅ Audit trail of upgrade acknowledgements

**Files to Modify**:
- `portal/backend/Models/ApplicationModule.cs`
- `portal/frontend/src/pages/ApplicationDetailsPage.tsx`
- `portal/backend/Controllers/ApplicationsController.cs`

---

#### 8.3 In-Portal Migration Guides
- [ ] Integrate migration guide template into portal
- [ ] Add `MigrationGuide` field to `ModuleVersion`
- [ ] Render markdown migration guide in portal UI
- [ ] Auto-populate guide with breaking changes from release notes
- [ ] Add syntax highlighting for code diffs

**Acceptance Criteria**:
- ✅ Migration guides visible in portal (no external docs needed)
- ✅ Guides include before/after code examples
- ✅ Markdown rendering works correctly

**Files to Modify**:
- `portal/backend/Models/ModuleVersion.cs`
- `portal/frontend/src/pages/ApplicationDetailsPage.tsx`
- `portal/backend/Controllers/ModulesController.cs`

---

## Milestone 9: Enhanced Portal Features
**Priority**: P2 - Medium  
**Timeline**: 2 weeks  
**Goal**: Improve portal usability and insights

### Tasks

#### 9.1 Module Adoption Dashboard
- [ ] Create dashboard showing module usage statistics
- [ ] Display: Total apps using module, version distribution, adoption trend
- [ ] Add filters: By technology stack, by module, by date range
- [ ] Add chart: Version adoption over time
- [ ] Make data exportable (CSV/Excel)

**Acceptance Criteria**:
- ✅ Dashboard shows actionable insights (which modules are popular)
- ✅ Charts are interactive and filterable
- ✅ Data updates in real-time

**Files to Create**:
- `portal/frontend/src/pages/AnalyticsPage.tsx`
- `portal/backend/Controllers/AnalyticsController.cs`

---

#### 9.2 Application Health Checks
- [ ] Add `/health` endpoint to portal API
- [ ] Check database connectivity
- [ ] Check Key Vault connectivity
- [ ] Return HTTP 200 if healthy, 503 if unhealthy
- [ ] Add health check UI in portal (show status of dependencies)

**Acceptance Criteria**:
- ✅ Health endpoint suitable for Azure App Service monitoring
- ✅ Unhealthy state clearly indicates which dependency failed

**Files to Create**:
- `portal/backend/Controllers/HealthController.cs`
- `portal/frontend/src/pages/StatusPage.tsx`

---

#### 9.3 Region/Compliance Tracking
- [ ] Add `Region` field to Application model (e.g., US-East, EU-West)
- [ ] Add `ComplianceRequirements` field (GDPR, HIPAA, SOC2)
- [ ] Display region in application details
- [ ] Add filter by region in applications list
- [ ] Document data residency implications

**Acceptance Criteria**:
- ✅ Applications tagged with deployment region
- ✅ Portal supports region-based filtering
- ✅ Compliance tracking visible in UI

**Files to Modify**:
- `portal/backend/Models/Application.cs`
- `portal/frontend/src/pages/ApplicationDetailsPage.tsx`
- `portal/frontend/src/pages/ApplicationsPage.tsx`

---

## Milestone 10: Security Enhancements
**Priority**: P3 - Low (Post-MVP)  
**Timeline**: 3-4 weeks  
**Goal**: Advanced security features for enterprise

### Tasks

#### 10.1 Audit Logging
- [ ] Create `AuditLog` table (Action, UserId, Timestamp, Details)
- [ ] Log all authentication attempts (success/failure)
- [ ] Log all configuration changes in portal
- [ ] Log all module version changes
- [ ] Make audit logs immutable (append-only)
- [ ] Add audit log viewer in portal (admin only)
- [ ] Export audit logs for compliance

**Acceptance Criteria**:
- ✅ All security-relevant events logged
- ✅ Logs tamper-proof
- ✅ Searchable and exportable

**Files to Create**:
- `portal/backend/Models/AuditLog.cs`
- `portal/backend/Services/AuditService.cs`
- `portal/frontend/src/pages/AuditLogPage.tsx`

---

#### 10.2 JWKS Key Rotation Support
- [ ] Monitor `kid` (key ID) changes in JWKS response
- [ ] Refresh JWKS cache when new key ID detected
- [ ] Support multiple active keys during rotation period
- [ ] Log key rotation events
- [ ] Add configuration for rotation detection interval

**Acceptance Criteria**:
- ✅ SDK automatically detects and handles key rotation
- ✅ No downtime during Azure AD key rotation

**Files to Modify**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator/JwksCache.cs`
- `sdk/nodejs/primus-identity-validator/src/cache/jwksCache.ts`

---

#### 10.3 Token Revocation Support (Optional)
- [ ] Research Azure AD token revocation APIs
- [ ] Implement optional revocation check
- [ ] Add configuration `EnableRevocationCheck`
- [ ] Cache revocation status to minimize API calls
- [ ] Document limitations (revocation not real-time)

**Acceptance Criteria**:
- ✅ SDK can check if token was revoked
- ✅ Feature is opt-in (disabled by default for performance)

**Files to Create**:
- `docs/TOKEN_REVOCATION.md`

**Files to Modify**:
- `sdk/dotnet/PrimusSaaS.Identity.Validator/AzureAdValidator.cs`

---

## Summary Timeline

| Milestone | Priority | Duration | Start After |
|---|---|---|---|
| **M1: Azure AD Core** | P0 | 3-4 weeks | Immediately |
| **M2: Multi-Tenancy** | P0 | 2-3 weeks | M1 complete |
| **M3: Security Hardening** | P0 | 2 weeks | M1 complete (can overlap with M2) |
| **M4: Documentation** | P1 | 2 weeks | M1 complete (can overlap) |
| **M5: Testing** | P1 | 2 weeks | M2 complete |
| **M6: CI/CD** | P1 | 2 weeks | M3 complete (can overlap with M5) |
| **M7: Advanced Auth** | P2 | 3-4 weeks | M6 complete |
| **M8: Upgrade Management** | P2 | 2-3 weeks | M6 complete (can overlap) |
| **M9: Portal Features** | P2 | 2 weeks | M8 complete |
| **M10: Security Enhancements** | P3 | 3-4 weeks | M9 complete |

**Total Estimated Time (Sequential)**: 21-28 weeks  
**Total Estimated Time (With Parallelization)**: 16-21 weeks

---

## Critical Path

**Phase 1 (MVP - Must Have)**: M1 → M2 → M3 → M5 → M6  
**Timeline**: 11-14 weeks  
**Deliverable**: Production-ready Azure AD authentication with multi-tenant support

**Phase 2 (Enhancement - Should Have)**: M4 → M7 → M8  
**Timeline**: 7-10 weeks  
**Deliverable**: Advanced features and upgrade automation

**Phase 3 (Polish - Nice to Have)**: M9 → M10  
**Timeline**: 5-8 weeks  
**Deliverable**: Enterprise features and security enhancements

---

## Next Steps

1. **Review & Prioritize**: Validate milestone priorities with stakeholders
2. **Resource Allocation**: Assign 2-3 engineers to Phase 1
3. **Sprint Planning**: Break down M1 into 2-week sprints
4. **Track Progress**: Use this document + JIRA/Azure DevOps for task tracking
5. **Weekly Checkpoints**: Review completed tasks against acceptance criteria

---

**Document Owner**: Platform Architecture Team  
**Last Updated**: November 15, 2025  
**Next Review**: After Milestone 1 Completion
