# Upgrade Workflow - Implementation Milestones

**Document Version**: 1.0  
**Date**: November 19, 2025  
**Scope**: Complete end-to-end upgrade workflow implementation

---

## Executive Summary

This document outlines the milestones and tasks required to achieve a complete end-to-end upgrade workflow as described in `docs/END_TO_END_UPGRADE_WORKFLOW.md`. Current implementation has basic upgrade functionality (60% complete) but lacks critical features for production readiness.

**Overall Status**: 60% Complete  
**Estimated Timeline**: 6-8 weeks for MVP, 10-12 weeks for full implementation

---

## Gap Analysis Summary

### ✅ Currently Implemented (60%)
- Portal database tracking of module versions
- Basic version comparison logic (current vs latest)
- Application Details page showing version status
- Upgrade Manager page with one-click upgrade
- Manual upgrade API endpoint
- Changelog display in UI
- Breaking change indicators
- Basic npm package publication workflow

### ❌ Missing Critical Features (40%)
- Automated npm/NuGet version synchronization
- Email/webhook notifications on version changes
- Rollback functionality
- Upgrade history/audit trail
- Client SDK version validation at runtime
- Automated dependency checks (breaking changes)
- CI/CD validation workflow
- Package registry webhooks integration
- Version migration guides/documentation generation
- Test coverage for upgrade scenarios

---

## Milestone 1: Version Synchronization & Notifications (P0 - Critical)
**Priority**: P0 - Blocker for production  
**Timeline**: 2-3 weeks  
**Goal**: Automate version detection and notify stakeholders

### 1.1 npm Registry Webhook Integration
**Status**: ❌ Not Started  
**Effort**: 3 days

#### Tasks
- [ ] Create webhook endpoint: `POST /api/webhooks/npm-registry`
- [ ] Implement webhook signature verification (npm secret)
- [ ] Parse npm registry payload (package name, version, published date)
- [ ] Auto-create ModuleVersion record when npm package published
- [ ] Map npm package names to portal modules (configuration table)
- [ ] Add webhook registration documentation

#### Implementation Details
```csharp
// portal/backend/Controllers/WebhooksController.cs (NEW FILE)
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    // POST: api/webhooks/npm-registry
    [HttpPost("npm-registry")]
    public async Task<IActionResult> HandleNpmWebhook([FromBody] NpmWebhookPayload payload)
    {
        // Verify webhook signature
        // Parse package version
        // Auto-create ModuleVersion record
        // Trigger notifications
    }
}
```

**Files to Create**:
- `portal/backend/Controllers/WebhooksController.cs`
- `portal/backend/Models/NpmWebhookPayload.cs`
- `portal/backend/Services/WebhookSignatureValidator.cs`
- `portal/backend/Migrations/AddPackageRegistryMappings.cs`

**Acceptance Criteria**:
- ✅ Webhook receives npm publish events within 5 seconds
- ✅ Signature verification prevents unauthorized requests
- ✅ ModuleVersion auto-created with correct metadata
- ✅ Webhook failures are logged and retried (3 attempts)
- ✅ Manual version creation still works (fallback)

---

### 1.2 Email Notification Service
**Status**: ❌ Not Started  
**Effort**: 4 days

#### Tasks
- [ ] Add email configuration (SMTP settings, SendGrid, etc.)
- [ ] Create email templates (new version, breaking changes, security fixes)
- [ ] Implement `IEmailService` interface
- [ ] Create `NotificationController` for notification management
- [ ] Add notification preferences per application owner
- [ ] Batch notifications (daily digest option)
- [ ] Track notification delivery status

#### Implementation Details
```csharp
// portal/backend/Services/IEmailService.cs (NEW FILE)
public interface IEmailService
{
    Task SendVersionUpdateNotificationAsync(Application app, ModuleVersion newVersion);
    Task SendBreakingChangeAlertAsync(Application app, ModuleVersion newVersion);
    Task SendSecurityUpdateAlertAsync(Application app, ModuleVersion newVersion);
}

// portal/backend/Models/NotificationPreference.cs (NEW FILE)
public class NotificationPreference
{
    public int ApplicationId { get; set; }
    public bool EmailOnNewVersion { get; set; } = true;
    public bool EmailOnBreakingChange { get; set; } = true;
    public bool DailyDigest { get; set; } = false;
    public string RecipientEmails { get; set; } = ""; // JSON array
}
```

**Files to Create**:
- `portal/backend/Services/IEmailService.cs`
- `portal/backend/Services/EmailService.cs`
- `portal/backend/Controllers/NotificationsController.cs`
- `portal/backend/Models/NotificationPreference.cs`
- `portal/backend/Templates/EmailTemplates/` (Razor templates)
- `portal/backend/Migrations/AddNotificationPreferences.cs`

**Email Templates**:
1. `NewVersionAvailable.cshtml` - Standard version update
2. `BreakingChangeAlert.cshtml` - Breaking change warning
3. `SecurityUpdateAlert.cshtml` - Urgent security fix
4. `DailyDigest.cshtml` - Summary of all updates

**Acceptance Criteria**:
- ✅ Email sent within 1 minute of version detection
- ✅ Emails contain: version numbers, changelog, installation commands
- ✅ Breaking changes highlighted in red with warning icon
- ✅ Security updates marked as "URGENT" with high priority
- ✅ Users can unsubscribe or change preferences
- ✅ Delivery failures are logged and retried

---

### 1.3 Webhook Notifications (Slack/Teams/Discord)
**Status**: ❌ Not Started  
**Effort**: 2 days

#### Tasks
- [ ] Add webhook URL configuration per application
- [ ] Implement Slack message formatting
- [ ] Implement Microsoft Teams adaptive cards
- [ ] Implement Discord embeds
- [ ] Add webhook test endpoint
- [ ] Document webhook payload format

#### Implementation Details
```csharp
// portal/backend/Models/WebhookConfiguration.cs (NEW FILE)
public class WebhookConfiguration
{
    public int ApplicationId { get; set; }
    public string WebhookUrl { get; set; } = "";
    public string WebhookType { get; set; } = ""; // Slack, Teams, Discord, Custom
    public bool NotifyOnNewVersion { get; set; } = true;
    public bool NotifyOnBreakingChange { get; set; } = true;
}
```

**Files to Create**:
- `portal/backend/Models/WebhookConfiguration.cs`
- `portal/backend/Services/IWebhookNotificationService.cs`
- `portal/backend/Services/WebhookNotificationService.cs`
- `portal/backend/Migrations/AddWebhookConfigurations.cs`

**Acceptance Criteria**:
- ✅ Slack webhooks deliver formatted messages with buttons
- ✅ Teams adaptive cards render correctly
- ✅ Discord embeds show version diff and changelog
- ✅ Test webhook endpoint verifies connectivity
- ✅ Webhook failures logged but don't block version creation

---

## Milestone 2: Upgrade History & Audit Trail (P1 - High)
**Priority**: P1 - Required for production  
**Timeline**: 1-2 weeks  
**Goal**: Track all upgrade operations and provide rollback capability

### 2.1 Upgrade History Tracking
**Status**: ❌ Not Started  
**Effort**: 3 days

#### Tasks
- [ ] Create `UpgradeHistory` database table
- [ ] Track: ApplicationId, ModuleId, FromVersion, ToVersion, Timestamp, UserId
- [ ] Add `UpgradeStatus` enum: Success, Failed, RolledBack
- [ ] Implement history API: `GET /api/applications/{id}/upgrade-history`
- [ ] Add history display in ApplicationDetailsPage UI
- [ ] Add filtering (by module, date range, status)

#### Database Schema
```csharp
// portal/backend/Models/UpgradeHistory.cs (NEW FILE)
public class UpgradeHistory
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int ModuleId { get; set; }
    public int FromVersionId { get; set; }
    public int ToVersionId { get; set; }
    public DateTime UpgradedAt { get; set; }
    public int UpgradedByUserId { get; set; }
    public string Status { get; set; } = "Success"; // Success, Failed, RolledBack
    public string Notes { get; set; } = "";
    
    // Navigation
    public Application Application { get; set; }
    public Module Module { get; set; }
    public ModuleVersion FromVersion { get; set; }
    public ModuleVersion ToVersion { get; set; }
    public User UpgradedBy { get; set; }
}
```

**Files to Create**:
- `portal/backend/Models/UpgradeHistory.cs`
- `portal/backend/Migrations/AddUpgradeHistory.cs`
- `portal/frontend/src/components/UpgradeHistoryTable.tsx`

**Acceptance Criteria**:
- ✅ Every upgrade operation logged automatically
- ✅ History displayed in Application Details page
- ✅ Filter by module, date range, status
- ✅ CSV export capability
- ✅ Audit trail immutable (no deletes, only inserts)

---

### 2.2 Rollback Functionality
**Status**: ❌ Not Started  
**Effort**: 3 days

#### Tasks
- [ ] Add rollback API: `POST /api/upgrade/applications/{appId}/modules/{moduleId}/rollback`
- [ ] Rollback restores previous ModuleVersionId from UpgradeHistory
- [ ] Add rollback button in UI (only for last upgrade)
- [ ] Create rollback confirmation modal
- [ ] Update UpgradeHistory status to "RolledBack"
- [ ] Send rollback notification email

#### Implementation Details
```csharp
// portal/backend/Controllers/UpgradeController.cs (MODIFY)
[HttpPost("applications/{applicationId}/modules/{moduleId}/rollback")]
public async Task<IActionResult> RollbackModule(int applicationId, int moduleId)
{
    // Get last upgrade from history
    var lastUpgrade = await _context.UpgradeHistory
        .Where(uh => uh.ApplicationId == applicationId && uh.ModuleId == moduleId)
        .OrderByDescending(uh => uh.UpgradedAt)
        .FirstOrDefaultAsync();
    
    if (lastUpgrade == null) return BadRequest("No upgrade history found");
    
    // Restore previous version
    var appModule = await _context.ApplicationModules
        .FirstOrDefaultAsync(am => am.ApplicationId == applicationId && am.ModuleId == moduleId);
    
    appModule.ModuleVersionId = lastUpgrade.FromVersionId;
    
    // Log rollback
    _context.UpgradeHistory.Add(new UpgradeHistory {
        ApplicationId = applicationId,
        ModuleId = moduleId,
        FromVersionId = lastUpgrade.ToVersionId,
        ToVersionId = lastUpgrade.FromVersionId,
        Status = "RolledBack"
    });
    
    await _context.SaveChangesAsync();
}
```

**Files to Modify**:
- `portal/backend/Controllers/UpgradeController.cs` (add rollback endpoint)
- `portal/frontend/src/pages/ApplicationDetailsPage.tsx` (add rollback button)

**Acceptance Criteria**:
- ✅ Rollback restores previous version successfully
- ✅ Rollback only available for last upgrade (prevent chain rollbacks)
- ✅ Confirmation modal prevents accidental rollbacks
- ✅ Rollback creates new history entry with status "RolledBack"
- ✅ Email notification sent on rollback

---

## Milestone 3: Client SDK Version Validation (P1 - High)
**Priority**: P1 - Required for production  
**Timeline**: 2-3 weeks  
**Goal**: Enable clients to verify version alignment at runtime

### 3.1 Health Check Endpoint in SDKs
**Status**: ❌ Not Started  
**Effort**: 4 days

#### Tasks
- [ ] Add version info to .NET SDK health check endpoint
- [ ] Add version info to Node.js SDK health check endpoint
- [ ] Include: SDK version, portal expected version, status (aligned/mismatched)
- [ ] Add portal API: `GET /api/applications/{id}/sdk-status`
- [ ] Compare SDK-reported version vs portal-tracked version

#### Implementation Details

**Node.js SDK**:
```typescript
// sdk/nodejs/primus-identity-validator/src/healthCheck.ts (NEW FILE)
export function getVersionInfo(portalApiUrl: string, clientId: string) {
  return {
    sdkVersion: require('../package.json').version,
    clientId: clientId,
    portalApiUrl: portalApiUrl
  };
}

// Express middleware
export function healthCheckMiddleware(portalApiUrl: string, clientId: string) {
  return (req, res) => {
    res.json({
      status: 'healthy',
      ...getVersionInfo(portalApiUrl, clientId)
    });
  };
}
```

**.NET SDK**:
```csharp
// sdk/dotnet/PrimusSaaS.Identity.Validator/HealthCheck.cs (NEW FILE)
public class IdentityValidatorHealthCheck : IHealthCheck
{
    private readonly IdentityValidatorOptions _options;
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context)
    {
        var version = typeof(IdentityValidatorHealthCheck).Assembly.GetName().Version?.ToString();
        var data = new Dictionary<string, object>
        {
            ["sdkVersion"] = version,
            ["clientId"] = _options.ClientId
        };
        return Task.FromResult(HealthCheckResult.Healthy("Identity validator is healthy", data));
    }
}
```

**Portal API**:
```csharp
// portal/backend/Controllers/ApplicationsController.cs (ADD ENDPOINT)
[HttpGet("{id}/sdk-status")]
public async Task<ActionResult<SdkStatusDto>> GetSdkStatus(int id)
{
    var app = await _context.Applications
        .Include(a => a.ApplicationModules)
        .ThenInclude(am => am.ModuleVersion)
        .FirstOrDefaultAsync(a => a.Id == id);
    
    // Client reports their version via health check callback
    // Compare against portal's tracked version
}
```

**Files to Create**:
- `sdk/nodejs/primus-identity-validator/src/healthCheck.ts`
- `sdk/nodejs/primus-identity-validator/src/__tests__/healthCheck.test.ts`
- `sdk/dotnet/PrimusSaaS.Identity.Validator/HealthCheck.cs`
- `sdk/dotnet/PrimusSaaS.Identity.Validator.Tests/HealthCheckTests.cs`
- `portal/backend/Models/SdkStatusDto.cs`

**Acceptance Criteria**:
- ✅ Health check endpoints return SDK version
- ✅ Portal compares reported vs expected version
- ✅ Mismatch warnings displayed in portal UI
- ✅ Health check response time < 100ms
- ✅ Tests cover version reporting logic

---

### 3.2 Startup Version Check
**Status**: ❌ Not Started  
**Effort**: 3 days

#### Tasks
- [ ] Add optional startup version check in .NET SDK
- [ ] Add optional startup version check in Node.js SDK
- [ ] Query portal API to get expected version
- [ ] Log warning if version mismatch detected
- [ ] Option to fail startup on version mismatch (configurable)

#### Implementation Details

**Node.js SDK**:
```typescript
// sdk/nodejs/primus-identity-validator/src/versionCheck.ts (NEW FILE)
export async function checkVersionOnStartup(config: {
  portalApiUrl: string;
  clientId: string;
  failOnMismatch?: boolean;
}) {
  const currentVersion = require('../package.json').version;
  
  const response = await fetch(`${config.portalApiUrl}/api/applications/by-client-id/${config.clientId}`);
  const appData = await response.json();
  
  const expectedVersion = appData.integratedModules
    .find(m => m.moduleName === 'IdentityValidator')?.version;
  
  if (currentVersion !== expectedVersion) {
    const message = `Version mismatch: running ${currentVersion}, portal expects ${expectedVersion}`;
    if (config.failOnMismatch) {
      throw new Error(message);
    } else {
      console.warn(message);
    }
  }
}
```

**.NET SDK**:
```csharp
// sdk/dotnet/PrimusSaaS.Identity.Validator/VersionCheck.cs (NEW FILE)
public static class VersionCheck
{
    public static async Task CheckVersionOnStartupAsync(IdentityValidatorOptions options)
    {
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        
        using var client = new HttpClient();
        var response = await client.GetAsync($"{options.PortalApiUrl}/api/applications/by-client-id/{options.ClientId}");
        var appData = await response.Content.ReadFromJsonAsync<ApplicationDto>();
        
        var expectedVersion = appData?.IntegratedModules
            .FirstOrDefault(m => m.ModuleName == "IdentityValidator")?.Version;
        
        if (currentVersion != expectedVersion)
        {
            var message = $"Version mismatch: running {currentVersion}, portal expects {expectedVersion}";
            if (options.FailOnVersionMismatch)
                throw new InvalidOperationException(message);
            else
                Console.WriteLine($"WARNING: {message}");
        }
    }
}
```

**Files to Create**:
- `sdk/nodejs/primus-identity-validator/src/versionCheck.ts`
- `sdk/dotnet/PrimusSaaS.Identity.Validator/VersionCheck.cs`

**Acceptance Criteria**:
- ✅ Startup check queries portal API successfully
- ✅ Version mismatch logs warning message
- ✅ Optional fail-on-mismatch throws error
- ✅ Startup check timeout after 5 seconds (doesn't block indefinitely)
- ✅ Startup check can be disabled via configuration

---

## Milestone 4: Breaking Change Detection & Migration (P2 - Medium)
**Priority**: P2 - Nice to have  
**Timeline**: 2 weeks  
**Goal**: Automated breaking change detection and migration guide generation

### 4.1 Breaking Change Dependency Analysis
**Status**: ❌ Not Started  
**Effort**: 4 days

#### Tasks
- [ ] Add breaking change type enum: ConfigChange, ApiChange, BehaviorChange
- [ ] Add affected features JSON to ModuleVersion
- [ ] Implement breaking change impact analysis
- [ ] Show affected applications in UI before upgrade
- [ ] Add "Review Breaking Changes" modal
- [ ] Require acknowledgment checkbox before breaking upgrade

#### Database Schema Extension
```csharp
// portal/backend/Models/ModuleVersion.cs (MODIFY)
public class ModuleVersion
{
    // ... existing fields
    public string BreakingChangeType { get; set; } = ""; // ConfigChange, ApiChange, BehaviorChange
    public string AffectedFeaturesJson { get; set; } = "[]"; // ["authentication", "token-validation"]
    public string MigrationGuide { get; set; } = ""; // Markdown migration instructions
}
```

**Files to Modify**:
- `portal/backend/Models/ModuleVersion.cs` (add breaking change metadata)
- `portal/backend/Migrations/AddBreakingChangeMetadata.cs`
- `portal/frontend/src/pages/UpgradeManagerPage.tsx` (add breaking change modal)

**Acceptance Criteria**:
- ✅ Breaking changes categorized by type
- ✅ Affected features listed in UI
- ✅ Acknowledgment required before breaking upgrade
- ✅ Migration guide displayed in modal
- ✅ Breaking upgrade count shown in dashboard

---

### 4.2 Automated Migration Guide Generation
**Status**: ❌ Not Started  
**Effort**: 3 days

#### Tasks
- [ ] Create migration guide template per breaking change type
- [ ] Auto-generate migration steps based on stack (Node.js vs .NET)
- [ ] Include before/after code examples
- [ ] Add migration guide to email notifications
- [ ] Add migration guide PDF export

#### Implementation Details
```csharp
// portal/backend/Services/MigrationGuideGenerator.cs (NEW FILE)
public class MigrationGuideGenerator
{
    public string GenerateMigrationGuide(ModuleVersion fromVersion, ModuleVersion toVersion, string stack)
    {
        // Parse breaking changes
        // Generate stack-specific migration steps
        // Include code examples
        // Return Markdown document
    }
}
```

**Files to Create**:
- `portal/backend/Services/MigrationGuideGenerator.cs`
- `portal/backend/Templates/MigrationGuides/` (Markdown templates)

**Acceptance Criteria**:
- ✅ Migration guides auto-generated for all breaking changes
- ✅ Stack-specific code examples included
- ✅ Migration guides available in portal UI and email
- ✅ PDF export available
- ✅ Migration guides stored in version history

---

## Milestone 5: CI/CD Integration & Validation (P2 - Medium)
**Priority**: P2 - Nice to have  
**Timeline**: 2 weeks  
**Goal**: Enable automated version validation in client CI/CD pipelines

### 5.1 Version Validation GitHub Action
**Status**: ❌ Not Started  
**Effort**: 3 days

#### Tasks
- [ ] Create GitHub Action: `primus-version-validator`
- [ ] Input: portal URL, client ID, API token
- [ ] Query portal for expected versions
- [ ] Compare package.json / .csproj versions
- [ ] Fail build if version mismatch detected
- [ ] Post comment on PR with version status

#### Implementation
```yaml
# .github/workflows/primus-version-check.yml (EXAMPLE)
name: Primus Version Check
on: [pull_request, push]

jobs:
  version-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: primus-saas/version-validator@v1
        with:
          portal-url: https://portal.primus.com
          client-id: ${{ secrets.PRIMUS_CLIENT_ID }}
          api-token: ${{ secrets.PRIMUS_API_TOKEN }}
          fail-on-mismatch: true
```

**Files to Create**:
- `.github/actions/primus-version-validator/action.yml`
- `.github/actions/primus-version-validator/index.js`
- `docs/CI_CD_INTEGRATION.md`

**Acceptance Criteria**:
- ✅ GitHub Action published to marketplace
- ✅ Action fails build on version mismatch
- ✅ PR comment shows version status
- ✅ Action runs in < 30 seconds
- ✅ Documentation includes setup guide

---

### 5.2 Dependabot Integration
**Status**: ❌ Not Started  
**Effort**: 2 days

#### Tasks
- [ ] Configure Dependabot for npm/NuGet packages
- [ ] Add Dependabot config to example projects
- [ ] Document Dependabot workflow
- [ ] Add portal integration to comment on Dependabot PRs

#### Implementation
```yaml
# .github/dependabot.yml (EXAMPLE)
version: 2
updates:
  - package-ecosystem: "npm"
    directory: "/"
    schedule:
      interval: "daily"
    groups:
      primus-saas:
        patterns:
          - "primus-identity-validator"
```

**Files to Create**:
- `.github/dependabot.yml` (example)
- `docs/DEPENDABOT_SETUP.md`

**Acceptance Criteria**:
- ✅ Dependabot creates PRs for new versions
- ✅ PR descriptions include changelog
- ✅ Breaking changes flagged in PR title
- ✅ Documentation covers setup process

---

## Milestone 6: Testing & Documentation (P1 - High)
**Priority**: P1 - Required for production  
**Timeline**: 2 weeks  
**Goal**: Comprehensive test coverage and documentation

### 6.1 Upgrade Workflow Integration Tests
**Status**: ❌ Not Started  
**Effort**: 4 days

#### Tasks
- [ ] Test: npm webhook triggers version creation
- [ ] Test: Email sent on new version
- [ ] Test: Upgrade API updates version correctly
- [ ] Test: Rollback restores previous version
- [ ] Test: Breaking change requires acknowledgment
- [ ] Test: Version mismatch detected at runtime
- [ ] Test: Upgrade history logged correctly
- [ ] Test: Multiple concurrent upgrades (race conditions)

**Files to Create**:
- `portal/backend.Tests/UpgradeWorkflowIntegrationTests.cs`
- `sdk/nodejs/primus-identity-validator/src/__tests__/integration/upgrade.test.ts`
- `sdk/dotnet/PrimusSaaS.Identity.Validator.Tests/UpgradeIntegrationTests.cs`

**Acceptance Criteria**:
- ✅ All upgrade scenarios covered (happy path, errors, rollbacks)
- ✅ Test coverage > 85% for upgrade code
- ✅ Integration tests run in CI/CD
- ✅ Performance tests validate upgrade latency < 2s

---

### 6.2 End-to-End Upgrade Documentation
**Status**: ✅ Completed (docs/END_TO_END_UPGRADE_WORKFLOW.md exists)  
**Effort**: Already done

#### Enhancements Needed
- [ ] Add screenshots of actual portal UI
- [ ] Add video walkthrough (Loom/YouTube)
- [ ] Add troubleshooting section
- [ ] Add FAQs
- [ ] Add version comparison matrix

**Files to Update**:
- `docs/END_TO_END_UPGRADE_WORKFLOW.md` (add screenshots, videos)
- `docs/TROUBLESHOOTING.md` (new file)
- `docs/FAQ.md` (update with upgrade FAQs)

**Acceptance Criteria**:
- ✅ Documentation includes screenshots
- ✅ Video walkthrough < 10 minutes
- ✅ Troubleshooting covers 10+ common issues
- ✅ FAQs answer all upgrade questions

---

## Milestone 7: Performance & Monitoring (P3 - Low)
**Priority**: P3 - Future enhancement  
**Timeline**: 1-2 weeks  
**Goal**: Optimize upgrade performance and add monitoring

### 7.1 Upgrade Performance Optimization
**Status**: ❌ Not Started  
**Effort**: 3 days

#### Tasks
- [ ] Add caching for version queries
- [ ] Optimize database queries (add indexes)
- [ ] Batch notification sending
- [ ] Implement async upgrade processing (queue-based)
- [ ] Add rate limiting to webhook endpoint

**Files to Modify**:
- `portal/backend/Controllers/UpgradeController.cs` (add caching)
- `portal/backend/Data/PortalDbContext.cs` (add indexes)
- `portal/backend/Services/NotificationQueue.cs` (new file)

**Acceptance Criteria**:
- ✅ Upgrade API response time < 200ms
- ✅ Notification sending queued (doesn't block API)
- ✅ Version queries cached for 5 minutes
- ✅ Webhook endpoint handles 100 req/min

---

### 7.2 Monitoring & Alerting
**Status**: ❌ Not Started  
**Effort**: 2 days

#### Tasks
- [ ] Add Application Insights / OpenTelemetry
- [ ] Track upgrade metrics (success rate, duration)
- [ ] Alert on upgrade failures
- [ ] Dashboard for upgrade analytics
- [ ] Track notification delivery rates

**Files to Create**:
- `portal/backend/Monitoring/UpgradeMetrics.cs`
- `portal/backend/appsettings.json` (add monitoring config)

**Acceptance Criteria**:
- ✅ All upgrade operations logged
- ✅ Alert triggered on >5% failure rate
- ✅ Dashboard shows upgrade trends
- ✅ Notification delivery rate > 95%

---

## Summary & Timeline

### Phase 1: MVP (Weeks 1-4) - P0 Priority
- **Week 1-2**: Milestone 1 (Version Sync & Notifications)
- **Week 3**: Milestone 2.1 (Upgrade History)
- **Week 4**: Milestone 3.1 (SDK Health Check)

**MVP Deliverables**:
- ✅ Automated version synchronization
- ✅ Email notifications on new versions
- ✅ Upgrade history tracking
- ✅ SDK version validation

### Phase 2: Production Ready (Weeks 5-8) - P1 Priority
- **Week 5**: Milestone 2.2 (Rollback) + Milestone 3.2 (Startup Check)
- **Week 6**: Milestone 4 (Breaking Change Detection)
- **Week 7-8**: Milestone 6 (Testing & Documentation)

**Production Deliverables**:
- ✅ Rollback functionality
- ✅ Breaking change workflows
- ✅ Comprehensive test coverage
- ✅ Complete documentation

### Phase 3: Enhancements (Weeks 9-12) - P2/P3 Priority
- **Week 9-10**: Milestone 5 (CI/CD Integration)
- **Week 11-12**: Milestone 7 (Performance & Monitoring)

**Enhancement Deliverables**:
- ✅ GitHub Actions integration
- ✅ Dependabot configuration
- ✅ Performance optimization
- ✅ Monitoring dashboards

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| npm webhook reliability | Medium | High | Implement fallback polling mechanism |
| Email delivery failures | Medium | Medium | Queue-based retry with 3 attempts |
| Version mismatch false positives | Low | Medium | Add manual override option |
| Breaking change detection accuracy | Medium | High | Require manual review before marking as breaking |
| Rollback data corruption | Low | Critical | Add database transaction isolation |
| CI/CD integration complexity | High | Medium | Provide comprehensive examples and docs |

---

## Success Metrics

| Metric | Target | Current |
|--------|--------|---------|
| Version sync latency | < 5 minutes | N/A (manual) |
| Notification delivery rate | > 95% | 0% (not implemented) |
| Upgrade success rate | > 99% | ~100% (limited testing) |
| Rollback success rate | > 95% | N/A (not implemented) |
| SDK version alignment | > 90% | Unknown (no tracking) |
| Documentation completeness | 100% | 80% (missing automation) |

---

## Next Steps

1. **Immediate Actions** (This Week):
   - Set up development environment for webhook testing
   - Configure email service (SendGrid/SMTP)
   - Begin Milestone 1.1 (npm webhook integration)

2. **Short-term** (Next 2 Weeks):
   - Complete Milestone 1 (Version Sync & Notifications)
   - Begin Milestone 2 (Upgrade History)

3. **Medium-term** (Next Month):
   - Complete MVP Phase (Milestones 1-3)
   - Begin production readiness testing

4. **Long-term** (Next Quarter):
   - Complete all P1 milestones
   - Deploy to production
   - Begin P2/P3 enhancements

---

## Appendix: Related Documents

- **Requirements**: `docs/END_TO_END_UPGRADE_WORKFLOW.md`
- **Architecture**: `docs/ARCHITECTURE.md` (Section 3: Update Awareness)
- **PRD**: `docs/PRD.md` (Section 5: Versioning & Upgrade Flow)
- **Current Progress**: `PROJECT_PROGRESS_STATUS.md`
- **Gap Analysis**: `docs/GAP_ANALYSIS.md`
- **Original Milestones**: `MILESTONES.md`

---

**Document Status**: DRAFT  
**Review Required**: Yes  
**Approved By**: _[Pending]_  
**Last Updated**: November 19, 2025
