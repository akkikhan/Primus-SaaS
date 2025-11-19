# Upgrade Workflow - Implementation TODO

**Version**: 1.0  
**Date**: November 19, 2025  
**Related**: See `UPGRADE_WORKFLOW_MILESTONES.md` for detailed task descriptions

---

## Quick Start Guide

This document provides a prioritized, actionable checklist for implementing the complete end-to-end upgrade workflow. Each task links to detailed specifications in the milestones document.

---

## Priority Overview

- **P0 (Critical - Weeks 1-4)**: Core automation and notifications - 11 tasks
- **P1 (High - Weeks 5-8)**: Production readiness features - 8 tasks
- **P2 (Medium - Weeks 9-10)**: CI/CD and enhancements - 6 tasks
- **P3 (Low - Weeks 11-12)**: Performance and monitoring - 5 tasks

**Total**: 30 implementation tasks

---

## 🔴 P0: Critical Features (MVP Blocker)

### Milestone 1: Version Synchronization & Notifications

#### 1.1 npm Registry Webhook Integration (3 days)
- [ ] Create `portal/backend/Controllers/WebhooksController.cs`
- [ ] Create `portal/backend/Models/NpmWebhookPayload.cs`
- [ ] Create `portal/backend/Services/WebhookSignatureValidator.cs`
- [ ] Add `POST /api/webhooks/npm-registry` endpoint
- [ ] Implement webhook signature verification
- [ ] Parse npm payload and auto-create ModuleVersion
- [ ] Create database migration: `AddPackageRegistryMappings`
- [ ] Add webhook configuration to appsettings.json
- [ ] Write webhook integration tests
- [ ] Document webhook registration process

**Acceptance**: Webhook receives npm publish events within 5 seconds

---

#### 1.2 Email Notification Service (4 days)
- [ ] Add email configuration to appsettings.json (SMTP/SendGrid)
- [ ] Create `portal/backend/Services/IEmailService.cs`
- [ ] Create `portal/backend/Services/EmailService.cs`
- [ ] Create `portal/backend/Models/NotificationPreference.cs`
- [ ] Create database migration: `AddNotificationPreferences`
- [ ] Create email template: `NewVersionAvailable.cshtml`
- [ ] Create email template: `BreakingChangeAlert.cshtml`
- [ ] Create email template: `SecurityUpdateAlert.cshtml`
- [ ] Create email template: `DailyDigest.cshtml`
- [ ] Create `portal/backend/Controllers/NotificationsController.cs`
- [ ] Add notification preferences API endpoints
- [ ] Trigger email on version creation
- [ ] Add email retry logic (3 attempts)
- [ ] Add unsubscribe functionality
- [ ] Write email service tests

**Acceptance**: Email sent within 1 minute of version detection

---

#### 1.3 Webhook Notifications (Slack/Teams/Discord) (2 days)
- [ ] Create `portal/backend/Models/WebhookConfiguration.cs`
- [ ] Create `portal/backend/Services/IWebhookNotificationService.cs`
- [ ] Create `portal/backend/Services/WebhookNotificationService.cs`
- [ ] Create database migration: `AddWebhookConfigurations`
- [ ] Implement Slack message formatter
- [ ] Implement Microsoft Teams adaptive card formatter
- [ ] Implement Discord embed formatter
- [ ] Add webhook test endpoint: `POST /api/notifications/test-webhook`
- [ ] Add webhook configuration UI in portal
- [ ] Write webhook notification tests

**Acceptance**: Slack/Teams/Discord notifications delivered with formatted content

---

### Milestone 2: Upgrade History & Audit Trail (Part 1)

#### 2.1 Upgrade History Tracking (3 days)
- [ ] Create `portal/backend/Models/UpgradeHistory.cs`
- [ ] Create database migration: `AddUpgradeHistory`
- [ ] Modify `UpgradeController.cs` to log all upgrade operations
- [ ] Add API: `GET /api/applications/{id}/upgrade-history`
- [ ] Add filtering (module, date range, status)
- [ ] Create `portal/frontend/src/components/UpgradeHistoryTable.tsx`
- [ ] Add upgrade history section to ApplicationDetailsPage
- [ ] Add CSV export functionality
- [ ] Write upgrade history tests

**Acceptance**: Every upgrade operation logged automatically with full audit trail

---

### Milestone 3: Client SDK Version Validation (Part 1)

#### 3.1 Health Check Endpoint in SDKs (4 days)
- [ ] Create `sdk/nodejs/primus-identity-validator/src/healthCheck.ts`
- [ ] Add health check middleware to Node.js SDK
- [ ] Write Node.js health check tests
- [ ] Create `sdk/dotnet/PrimusSaaS.Identity.Validator/HealthCheck.cs`
- [ ] Implement IHealthCheck interface for .NET SDK
- [ ] Write .NET health check tests
- [ ] Add API: `GET /api/applications/{id}/sdk-status`
- [ ] Create `portal/backend/Models/SdkStatusDto.cs`
- [ ] Add SDK status display in ApplicationDetailsPage
- [ ] Add version mismatch warning badge
- [ ] Document health check setup for clients

**Acceptance**: Health check returns SDK version, portal detects mismatches

---

## 🟡 P1: Production Readiness Features

### Milestone 2: Upgrade History & Audit Trail (Part 2)

#### 2.2 Rollback Functionality (3 days)
- [ ] Add API: `POST /api/upgrade/applications/{appId}/modules/{moduleId}/rollback`
- [ ] Implement rollback logic in UpgradeController
- [ ] Create rollback history entry with status "RolledBack"
- [ ] Add rollback button to ApplicationDetailsPage
- [ ] Create rollback confirmation modal
- [ ] Add rollback notification email template
- [ ] Trigger rollback notification
- [ ] Add rollback tests (success, failure scenarios)
- [ ] Add rollback constraints (only last upgrade)

**Acceptance**: Rollback restores previous version with confirmation modal

---

### Milestone 3: Client SDK Version Validation (Part 2)

#### 3.2 Startup Version Check (3 days)
- [ ] Create `sdk/nodejs/primus-identity-validator/src/versionCheck.ts`
- [ ] Implement version check in Node.js SDK startup
- [ ] Add `failOnVersionMismatch` configuration option (Node.js)
- [ ] Create `sdk/dotnet/PrimusSaaS.Identity.Validator/VersionCheck.cs`
- [ ] Implement version check in .NET SDK startup
- [ ] Add `FailOnVersionMismatch` configuration option (.NET)
- [ ] Add API: `GET /api/applications/by-client-id/{clientId}`
- [ ] Add timeout handling (5 seconds max)
- [ ] Write version check tests
- [ ] Document startup version check setup

**Acceptance**: SDK logs warning/error on version mismatch at startup

---

### Milestone 4: Breaking Change Detection & Migration

#### 4.1 Breaking Change Dependency Analysis (4 days)
- [ ] Add fields to ModuleVersion: `BreakingChangeType`, `AffectedFeaturesJson`, `MigrationGuide`
- [ ] Create database migration: `AddBreakingChangeMetadata`
- [ ] Update ModulesController to accept breaking change metadata
- [ ] Add breaking change type enum: ConfigChange, ApiChange, BehaviorChange
- [ ] Create breaking change review modal in UpgradeManagerPage
- [ ] Add "affected applications" count before upgrade
- [ ] Add acknowledgment checkbox for breaking upgrades
- [ ] Display migration guide in modal
- [ ] Add breaking change count to dashboard
- [ ] Write breaking change workflow tests

**Acceptance**: Breaking changes require acknowledgment before upgrade

---

#### 4.2 Automated Migration Guide Generation (3 days)
- [ ] Create `portal/backend/Services/MigrationGuideGenerator.cs`
- [ ] Create migration guide template: ConfigChange
- [ ] Create migration guide template: ApiChange
- [ ] Create migration guide template: BehaviorChange
- [ ] Generate stack-specific code examples (Node.js, .NET)
- [ ] Add migration guide to email notifications
- [ ] Add migration guide PDF export
- [ ] Store migration guides in UpgradeHistory
- [ ] Write migration guide generator tests

**Acceptance**: Migration guides auto-generated with code examples

---

### Milestone 6: Testing & Documentation

#### 6.1 Upgrade Workflow Integration Tests (4 days)
- [ ] Create `portal/backend.Tests/UpgradeWorkflowIntegrationTests.cs`
- [ ] Test: npm webhook triggers version creation
- [ ] Test: Email sent on new version
- [ ] Test: Upgrade API updates version correctly
- [ ] Test: Rollback restores previous version
- [ ] Test: Breaking change requires acknowledgment
- [ ] Test: Version mismatch detected at runtime
- [ ] Test: Upgrade history logged correctly
- [ ] Test: Multiple concurrent upgrades (race conditions)
- [ ] Create `sdk/nodejs/primus-identity-validator/src/__tests__/integration/upgrade.test.ts`
- [ ] Create `sdk/dotnet/PrimusSaaS.Identity.Validator.Tests/UpgradeIntegrationTests.cs`
- [ ] Add performance tests (upgrade latency < 2s)
- [ ] Run tests in CI/CD pipeline

**Acceptance**: Test coverage > 85% for upgrade code

---

#### 6.2 End-to-End Documentation Enhancements (2 days)
- [ ] Add screenshots to `docs/END_TO_END_UPGRADE_WORKFLOW.md`
- [ ] Record video walkthrough (10 minutes max)
- [ ] Create `docs/TROUBLESHOOTING.md`
- [ ] Add 10+ common upgrade issues with solutions
- [ ] Update `docs/FAQ.md` with upgrade questions
- [ ] Create version comparison matrix table
- [ ] Add "Getting Started" quick guide
- [ ] Review and update all code examples

**Acceptance**: Documentation includes screenshots and video walkthrough

---

## 🟢 P2: CI/CD Integration & Enhancements

### Milestone 5: CI/CD Integration & Validation

#### 5.1 Version Validation GitHub Action (3 days)
- [ ] Create `.github/actions/primus-version-validator/action.yml`
- [ ] Create `.github/actions/primus-version-validator/index.js`
- [ ] Implement version query to portal API
- [ ] Compare package.json / .csproj versions
- [ ] Fail build on version mismatch (configurable)
- [ ] Post PR comment with version status
- [ ] Publish action to GitHub Marketplace
- [ ] Create example workflow: `.github/workflows/primus-version-check.yml`
- [ ] Create `docs/CI_CD_INTEGRATION.md`
- [ ] Write action tests

**Acceptance**: GitHub Action fails build on version mismatch, posts PR comment

---

#### 5.2 Dependabot Integration (2 days)
- [ ] Create example `.github/dependabot.yml`
- [ ] Configure Dependabot for npm packages
- [ ] Configure Dependabot for NuGet packages
- [ ] Add Dependabot to all example projects
- [ ] Create `docs/DEPENDABOT_SETUP.md`
- [ ] Document Dependabot workflow
- [ ] Test Dependabot PR creation
- [ ] Add portal integration for Dependabot PR comments

**Acceptance**: Dependabot creates PRs for new versions with changelog

---

#### 5.3 npm/NuGet Package Comparison Tool (2 days)
- [ ] Create CLI tool: `primus-version-check`
- [ ] Query npm registry for package versions
- [ ] Query NuGet gallery for package versions
- [ ] Compare local versions with registry
- [ ] Output version diff table
- [ ] Add JSON output option
- [ ] Publish to npm/NuGet
- [ ] Add usage documentation

**Acceptance**: CLI tool reports version diff in < 5 seconds

---

## 🔵 P3: Performance & Monitoring

### Milestone 7: Performance & Monitoring

#### 7.1 Upgrade Performance Optimization (3 days)
- [ ] Add Redis caching for version queries
- [ ] Add database indexes: ModuleVersions (ModuleId, ReleasedAt)
- [ ] Add database index: ApplicationModules (ApplicationId, ModuleId)
- [ ] Implement async upgrade processing with queue (Hangfire/Azure Service Bus)
- [ ] Batch notification sending (max 10 concurrent)
- [ ] Add rate limiting to webhook endpoint (100 req/min)
- [ ] Optimize UpgradeController queries (reduce N+1)
- [ ] Write performance tests (load testing)

**Acceptance**: Upgrade API response < 200ms, notifications queued

---

#### 7.2 Monitoring & Alerting (2 days)
- [ ] Add Application Insights SDK
- [ ] Add custom metrics: upgrade_success_count, upgrade_failure_count
- [ ] Add custom metrics: notification_delivery_count, notification_failure_count
- [ ] Create `portal/backend/Monitoring/UpgradeMetrics.cs`
- [ ] Add alert: upgrade failure rate > 5%
- [ ] Add alert: notification delivery rate < 95%
- [ ] Create dashboard: upgrade trends (Grafana/App Insights)
- [ ] Add distributed tracing for upgrade operations

**Acceptance**: All operations logged, alerts triggered on failures

---

#### 7.3 Automated Version Polling (Fallback) (2 days)
- [ ] Create background service: `NpmVersionPollingService`
- [ ] Query npm registry every 15 minutes
- [ ] Compare npm versions vs portal versions
- [ ] Create ModuleVersion if mismatch detected
- [ ] Add configuration: polling interval, enabled/disabled
- [ ] Add NuGet version polling
- [ ] Log polling results
- [ ] Write polling service tests

**Acceptance**: Fallback polling detects versions within 15 minutes if webhook fails

---

## Implementation Strategy

### Phase 1: MVP (Weeks 1-4)
**Focus**: Core automation and notifications

**Week 1**:
- [ ] Day 1-2: Task 1.1 (npm webhook) - setup and basic implementation
- [ ] Day 3-5: Task 1.1 (npm webhook) - testing and documentation

**Week 2**:
- [ ] Day 1-3: Task 1.2 (email service) - implementation
- [ ] Day 4-5: Task 1.2 (email service) - templates and testing

**Week 3**:
- [ ] Day 1-2: Task 1.3 (webhook notifications)
- [ ] Day 3-5: Task 2.1 (upgrade history)

**Week 4**:
- [ ] Day 1-4: Task 3.1 (SDK health check)
- [ ] Day 5: Testing and bug fixes

**MVP Checkpoint**: Review and demo to stakeholders

---

### Phase 2: Production (Weeks 5-8)
**Focus**: Rollback, breaking changes, testing

**Week 5**:
- [ ] Day 1-3: Task 2.2 (rollback functionality)
- [ ] Day 4-5: Task 3.2 (startup version check) - begin

**Week 6**:
- [ ] Day 1: Task 3.2 (startup version check) - complete
- [ ] Day 2-5: Task 4.1 (breaking change detection)

**Week 7**:
- [ ] Day 1-3: Task 4.2 (migration guide generation)
- [ ] Day 4-5: Task 6.1 (integration tests) - begin

**Week 8**:
- [ ] Day 1-3: Task 6.1 (integration tests) - complete
- [ ] Day 4-5: Task 6.2 (documentation enhancements)

**Production Checkpoint**: Security review and load testing

---

### Phase 3: Enhancements (Weeks 9-12)
**Focus**: CI/CD, performance, monitoring

**Week 9**:
- [ ] Day 1-3: Task 5.1 (GitHub Action)
- [ ] Day 4-5: Task 5.2 (Dependabot)

**Week 10**:
- [ ] Day 1-2: Task 5.3 (version comparison tool)
- [ ] Day 3-5: Task 7.1 (performance optimization)

**Week 11**:
- [ ] Day 1-2: Task 7.2 (monitoring)
- [ ] Day 3-4: Task 7.3 (automated polling)
- [ ] Day 5: Final testing

**Week 12**:
- [ ] Day 1-3: Bug fixes and refinements
- [ ] Day 4: Final documentation review
- [ ] Day 5: Production deployment

---

## Quick Reference: Files to Create

### Backend (15 new files)
- `Controllers/WebhooksController.cs`
- `Controllers/NotificationsController.cs`
- `Models/NpmWebhookPayload.cs`
- `Models/WebhookConfiguration.cs`
- `Models/NotificationPreference.cs`
- `Models/UpgradeHistory.cs`
- `Models/SdkStatusDto.cs`
- `Services/WebhookSignatureValidator.cs`
- `Services/IEmailService.cs`
- `Services/EmailService.cs`
- `Services/IWebhookNotificationService.cs`
- `Services/WebhookNotificationService.cs`
- `Services/MigrationGuideGenerator.cs`
- `Services/NotificationQueue.cs`
- `Monitoring/UpgradeMetrics.cs`

### Frontend (2 new files)
- `components/UpgradeHistoryTable.tsx`
- `components/BreakingChangeModal.tsx`

### SDK Node.js (3 new files)
- `src/healthCheck.ts`
- `src/versionCheck.ts`
- `src/__tests__/integration/upgrade.test.ts`

### SDK .NET (3 new files)
- `HealthCheck.cs`
- `VersionCheck.cs`
- `Tests/UpgradeIntegrationTests.cs`

### Documentation (4 new files)
- `docs/TROUBLESHOOTING.md`
- `docs/CI_CD_INTEGRATION.md`
- `docs/DEPENDABOT_SETUP.md`
- `.github/workflows/primus-version-check.yml`

### Total: 27 new files + 10 modified files

---

## Database Migrations Required

1. `AddPackageRegistryMappings` - npm/NuGet package mapping
2. `AddNotificationPreferences` - email notification settings
3. `AddWebhookConfigurations` - webhook URLs
4. `AddUpgradeHistory` - upgrade audit trail
5. `AddBreakingChangeMetadata` - breaking change details
6. (Optional) Add indexes for performance

---

## Configuration Updates Needed

### appsettings.json
```json
{
  "Email": {
    "Provider": "SendGrid",
    "ApiKey": "...",
    "FromAddress": "noreply@primus.com"
  },
  "Webhooks": {
    "NpmSecret": "...",
    "RateLimitPerMinute": 100
  },
  "VersionSync": {
    "PollingEnabled": false,
    "PollingIntervalMinutes": 15
  },
  "Monitoring": {
    "ApplicationInsightsKey": "..."
  }
}
```

---

## Testing Checklist

### Unit Tests (20 test classes)
- [ ] WebhooksController tests
- [ ] EmailService tests
- [ ] WebhookNotificationService tests
- [ ] UpgradeController tests (rollback)
- [ ] MigrationGuideGenerator tests
- [ ] Node.js health check tests
- [ ] Node.js version check tests
- [ ] .NET health check tests
- [ ] .NET version check tests

### Integration Tests (8 scenarios)
- [ ] End-to-end webhook → email → upgrade
- [ ] Breaking change workflow
- [ ] Rollback workflow
- [ ] Version mismatch detection
- [ ] Concurrent upgrades
- [ ] Notification retry logic
- [ ] Performance (1000 upgrades)
- [ ] CI/CD GitHub Action

### Manual Testing (5 scenarios)
- [ ] Portal UI upgrade workflow
- [ ] Email templates rendering
- [ ] Slack/Teams notifications
- [ ] SDK version check logs
- [ ] Migration guide generation

---

## Dependencies & Prerequisites

### Required Before Starting
- [ ] SendGrid API key (or SMTP credentials)
- [ ] npm webhook secret generation
- [ ] Redis instance (for caching)
- [ ] Application Insights workspace (optional)
- [ ] Test npm package for webhook testing
- [ ] Test applications for integration testing

### External Services
- npm registry (webhooks)
- SendGrid / SMTP server
- Redis (caching)
- Application Insights (monitoring)
- Slack/Teams/Discord (webhooks)

---

## Progress Tracking

**Overall**: 0/30 tasks completed (0%)

- **P0 Critical**: 0/11 tasks (0%)
- **P1 High**: 0/8 tasks (0%)
- **P2 Medium**: 0/6 tasks (0%)
- **P3 Low**: 0/5 tasks (0%)

**Last Updated**: November 19, 2025  
**Next Review**: [Schedule weekly review]

---

## Notes & Decisions Log

| Date | Decision | Rationale | Impact |
|------|----------|-----------|--------|
| 2025-11-19 | Use SendGrid for email | Reliable delivery, templates | Need API key |
| 2025-11-19 | Queue-based notifications | Don't block API calls | Need background service |
| 2025-11-19 | Optional startup check | Some clients may not want it | Configurable |
| 2025-11-19 | Rollback only last upgrade | Prevent chain complexity | Simplified logic |

---

## Related Documents

- **Milestones**: `UPGRADE_WORKFLOW_MILESTONES.md` (detailed specifications)
- **Requirements**: `docs/END_TO_END_UPGRADE_WORKFLOW.md`
- **Progress**: `PROJECT_PROGRESS_STATUS.md`
- **Architecture**: `docs/ARCHITECTURE.md`

---

**Status**: READY FOR IMPLEMENTATION  
**Approved**: [Pending]  
**Start Date**: [To be scheduled]
