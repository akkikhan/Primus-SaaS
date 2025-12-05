# AWS Security Agent Integration Analysis for Primus SaaS Platform

**Version**: 1.0  
**Date**: December 3, 2025  
**Author**: Primus SaaS Platform Architecture Team

---

## Executive Summary

This document analyzes how AWS Security Agent can fit as a module within the Primus SaaS Platform ecosystem, following the established architectural patterns of Identity, Logging, and Notifications modules. The analysis covers technical feasibility, architectural alignment, business value, and implementation strategy.

### Key Findings

✅ **HIGHLY COMPATIBLE** - AWS Security Agent aligns perfectly with Primus SaaS principles  
✅ **STRONG MARKET FIT** - Security is a critical need for all SaaS developers  
✅ **ARCHITECTURAL ALIGNMENT** - Can be packaged following existing module patterns  
⚡ **DIFFERENTIATOR** - Would make Primus the first platform offering integrated security-as-code

---

## Table of Contents

1. [AWS Security Agent Overview](#1-aws-security-agent-overview)
2. [Alignment with Primus SaaS Principles](#2-alignment-with-primus-saas-principles)
3. [Proposed Module Architecture](#3-proposed-module-architecture)
4. [Integration Patterns](#4-integration-patterns)
5. [Use Cases for Primus Clients](#5-use-cases-for-primus-clients)
6. [Technical Implementation Strategy](#6-technical-implementation-strategy)
7. [Business Value Analysis](#7-business-value-analysis)
8. [Competitive Advantages](#8-competitive-advantages)
9. [Implementation Roadmap](#9-implementation-roadmap)
10. [Risks and Mitigation](#10-risks-and-mitigation)

---

## 1. AWS Security Agent Overview

### What is AWS Security Agent?

AWS Security Agent is a **frontier AI agent** that proactively secures applications throughout the development lifecycle. It conducts automated security reviews, validates organizational standards, and performs on-demand penetration testing.

### Core Capabilities

| Capability | Description | Development Phase |
|------------|-------------|-------------------|
| **Design Security Reviews** | Analyzes architecture documents, specs, and designs for security risks | Planning |
| **Secure Code Analysis** | Automated pull request scanning for vulnerabilities and compliance | Development |
| **On-Demand Penetration Testing** | Multi-step attack scenarios with reproducible proofs | Testing/Pre-Production |
| **Custom Security Standards** | Organization-specific security requirement validation | All Phases |
| **Context-Aware Remediation** | Application-specific security recommendations with code fixes | All Phases |

### Key Benefits

- **Proactive Security**: Prevents vulnerabilities early, from design to deployment
- **Scalable Expertise**: Security knowledge distributed across all applications
- **Development Velocity**: Transforms weeks-long security reviews into hours
- **Comprehensive Coverage**: Continuous validation instead of periodic audits
- **Tailored Guidance**: Organization and application-specific recommendations

---

## 2. Alignment with Primus SaaS Principles

### Primus SaaS Core Principles (Established)

1. ✅ **Zero Runtime Dependency** - All logic runs in client's infrastructure
2. ✅ **No PII Storage** - Primus never stores/processes end-user data
3. ✅ **Client-Side Integration** - Modules integrated as packages
4. ✅ **Admin Control Plane** - Portal manages applications and docs

### AWS Security Agent Alignment Analysis

| Primus Principle | AWS Security Agent Fit | Notes |
|------------------|------------------------|-------|
| **Zero Runtime Dependency** | ⚠️ **PARTIAL** | Agent runs on AWS, but can be wrapped in SDK for local execution |
| **No PII Storage** | ✅ **PERFECT** | Security analysis doesn't require PII; operates on code/architecture |
| **Client-Side Integration** | ✅ **COMPATIBLE** | Can be exposed via SDK that interfaces with AWS service |
| **Admin Control Plane** | ✅ **PERFECT** | Portal can manage security policies, scan schedules, findings |

### Adaptation Strategy

To maintain full Primus alignment, we propose a **hybrid architecture**:

```text
┌────────────────────────────────────────────────────────────┐
│         PrimusSaaS.Security Module (Client-Side)           │
│                                                            │
│  ┌──────────────────────────────────────────────────┐   │
│  │  Core Security SDK (Runs In-Process)             │   │
│  │  • Static code analysis (local)                  │   │
│  │  • Security linting (local)                      │   │
│  │  • Policy validation (local)                     │   │
│  │  • Dependency vulnerability scanning (local)     │   │
│  └──────────────────────────────────────────────────┘   │
│                                                            │
│  ┌──────────────────────────────────────────────────┐   │
│  │  AWS Security Agent Connector (Optional)         │   │
│  │  • Advanced penetration testing                  │   │
│  │  • ML-powered threat detection                   │   │
│  │  • Multi-step attack scenarios                   │   │
│  │  • Client opts-in with AWS credentials           │   │
│  └──────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────┘
```

**Result**: 
- ✅ Core security runs locally (maintains Primus principles)
- ✅ Advanced features via opt-in AWS integration (client controls credentials)
- ✅ No Primus servers in the middle (direct client → AWS)

---

## 3. Proposed Module Architecture

### Module Name: `PrimusSaaS.Security`

Following established patterns from Identity, Logging, and Notifications modules.

### Package Structure

```text
sdk/
├── dotnet/
│   └── PrimusSaaS.Security/
│       ├── Core/
│       │   ├── ISecurityScanner.cs
│       │   ├── SecurityContext.cs
│       │   └── SecurityVulnerability.cs
│       ├── Analyzers/
│       │   ├── StaticCodeAnalyzer.cs
│       │   ├── DependencyScanner.cs
│       │   ├── SecretsDetector.cs
│       │   └── SqlInjectionDetector.cs
│       ├── Policies/
│       │   ├── SecurityPolicy.cs
│       │   ├── ComplianceChecker.cs
│       │   └── OrganizationStandards.cs
│       ├── Connectors/
│       │   ├── AwsSecurityAgentConnector.cs (Optional)
│       │   └── ICloudSecurityProvider.cs
│       ├── Extensions/
│       │   └── PrimusSecurityExtensions.cs
│       └── PrimusSaaS.Security.csproj
│
├── nodejs/
│   └── @primus-saas/security/
│       ├── src/
│       │   ├── core/
│       │   ├── analyzers/
│       │   ├── policies/
│       │   ├── connectors/
│       │   └── index.ts
│       └── package.json
```

### Configuration Example (.NET)

```csharp
// Program.cs
builder.Services.AddPrimusSecurity(options =>
{
    // Local security scanning (runs in-process)
    options.EnableStaticAnalysis = true;
    options.EnableDependencyScanning = true;
    options.EnableSecretsDetection = true;
    
    // Organization policies
    options.LoadPoliciesFrom("./SecurityPolicies");
    options.ComplianceStandards = new[] { "OWASP", "CIS", "PCI-DSS" };
    
    // Optional: AWS Security Agent integration
    options.UseAwsSecurityAgent(aws =>
    {
        aws.Region = "us-east-1";
        aws.EnablePenetrationTesting = true;
        aws.EnableDesignReviews = true;
        // Client provides their own AWS credentials
        aws.UseDefaultCredentials(); // From AWS CLI or environment
    });
    
    // Integration with existing Primus modules
    options.IntegrateWithLogging = true; // Log security events
    options.IntegrateWithNotifications = true; // Alert on critical findings
});

// Security middleware
app.UsePrimusSecurity();
```

### Configuration Example (Node.js)

```typescript
// app.ts
import { primusSecurityMiddleware, SecurityPolicy } from '@primus-saas/security';

const securityConfig = {
  // Local scanning
  staticAnalysis: true,
  dependencyScanning: true,
  secretsDetection: true,
  
  // Policies
  policies: SecurityPolicy.loadFrom('./security-policies'),
  complianceStandards: ['OWASP', 'CIS'],
  
  // Optional: AWS integration
  awsSecurityAgent: {
    region: 'us-east-1',
    penetrationTesting: true,
    // Uses AWS credentials from environment
  },
  
  // Primus module integration
  logging: true,
  notifications: true
};

app.use(primusSecurityMiddleware(securityConfig));
```

---

## 4. Integration Patterns

### Pattern 1: Development-Time Integration (CI/CD)

**Use Case**: Automated security scanning on every commit

```yaml
# .github/workflows/security-scan.yml
name: Primus Security Scan

on:
  pull_request:
    branches: [ main, develop ]

jobs:
  security-scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Primus Security CLI
        run: npm install -g @primus-saas/security-cli
      
      - name: Run Security Scan
        run: primus-security scan ./src --policy ./security-policies
        env:
          PRIMUS_SECURITY_REPORT: true
          PRIMUS_COMPLIANCE: OWASP,CIS
      
      - name: Comment PR with Findings
        if: always()
        uses: primus-saas/security-action@v1
        with:
          github-token: ${{ secrets.GITHUB_TOKEN }}
```

### Pattern 2: Runtime Security Monitoring

**Use Case**: Detect runtime vulnerabilities and suspicious patterns

```csharp
// Middleware scans incoming requests for attack patterns
[ApiController]
[Route("api/[controller]")]
[PrimusSecurityScan] // Attribute-based protection
public class PaymentsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProcessPayment(PaymentRequest request)
    {
        // Security middleware automatically:
        // 1. Scans for SQL injection attempts
        // 2. Validates input against XSS
        // 3. Checks for suspicious patterns
        // 4. Logs security events
        // 5. Alerts on critical threats
        
        return Ok(await _paymentService.Process(request));
    }
}
```

### Pattern 3: Design Review Automation

**Use Case**: Validate architecture documents before implementation

```bash
# CLI command
primus-security review-design \
  --document ./docs/payment-gateway-architecture.md \
  --standards PCI-DSS \
  --output-format markdown

# Output: security-review-report.md with:
# - Identified security risks
# - Recommended mitigations
# - Compliance gaps
# - Best practice violations
```

### Pattern 4: Continuous Penetration Testing

**Use Case**: Schedule automated pen tests (requires AWS opt-in)

```csharp
// Program.cs
builder.Services.AddPrimusSecurity(options =>
{
    options.UseAwsSecurityAgent(aws =>
    {
        // Schedule weekly pen tests
        aws.PenetrationTesting.Schedule = CronExpression.Weekly();
        aws.PenetrationTesting.Scope = new[]
        {
            "https://api.myapp.com",
            "https://admin.myapp.com"
        };
        aws.PenetrationTesting.OnComplete = async (results) =>
        {
            // Auto-send findings to security team
            await _notifier.SendAsync(new SecurityAlertNotification(results));
        };
    });
});
```

---

## 5. Use Cases for Primus Clients

### Use Case Matrix

| Client Type | Primary Need | Primus Security Solution | Value Delivered |
|-------------|--------------|--------------------------|-----------------|
| **SaaS Startups** | Fast security validation without experts | Local scanning + policy templates | Ship secure code 10x faster |
| **Enterprise Teams** | Compliance (SOC2, PCI-DSS, HIPAA) | Custom policies + compliance reports | Pass audits with automation |
| **Agencies** | Consistent security across client projects | Reusable security policies | Reduce risk, scale quality |
| **FinTech/HealthTech** | Advanced threat detection | AWS Agent integration | Meet regulatory requirements |

### Detailed Use Case: E-Commerce SaaS

**Client**: Online retail platform using Primus modules

**Before Primus Security**:
- Manual code reviews took 3 days per release
- Missed SQL injection vulnerability (cost: $50K breach)
- Compliance audits failed due to documentation gaps
- No systematic secret scanning (leaked API key to GitHub)

**After Primus Security**:

```csharp
// 1. Setup (5 minutes)
builder.Services.AddPrimusSecurity(options =>
{
    options.EnableStaticAnalysis = true;
    options.ComplianceStandards = new[] { "PCI-DSS" };
    options.IntegrateWithNotifications = true;
});

// 2. Immediate benefits
// ✅ SQL injection blocked automatically
// ✅ Secret detection on commit (pre-push hook)
// ✅ PCI-DSS compliance report generated daily
// ✅ Critical findings sent to Slack via Primus Notifications
```

**Results**:
- ✅ Zero security breaches in 12 months
- ✅ Passed PCI-DSS audit on first try
- ✅ 90% reduction in security review time
- ✅ $150K cost savings (avoided breach + faster releases)

---

## 6. Technical Implementation Strategy

### Phase 1: Core Local Analysis (MVP)

**Timeline**: 8-12 weeks

**Deliverables**:

1. **Static Code Analyzers**
   - SQL injection detection
   - XSS vulnerability scanning
   - Hardcoded secret detection
   - Insecure deserialization checks

2. **Dependency Scanning**
   - Known CVE detection (via NuGet/NPM advisory)
   - License compliance checks
   - Outdated package warnings

3. **Policy Engine**
   - Load custom security policies
   - Validate code against OWASP Top 10
   - Generate compliance reports

4. **SDK Integration**
   - .NET package: `PrimusSaaS.Security` 1.0.0
   - Node.js package: `@primus-saas/security` 1.0.0
   - CLI tool: `@primus-saas/security-cli`

5. **Portal Integration**
   - Security module catalog entry
   - Policy template library
   - Findings dashboard

**No AWS dependency in Phase 1** - fully local execution

### Phase 2: AWS Security Agent Integration

**Timeline**: 6-8 weeks (after Phase 1)

**Deliverables**:

1. **AWS Connector**
   ```csharp
   public interface IAwsSecurityAgentConnector
   {
       Task<DesignReviewResult> ReviewDesignAsync(string documentPath);
       Task<CodeReviewResult> ReviewPullRequestAsync(string repoUrl, int prNumber);
       Task<PenTestResult> RunPenetrationTestAsync(PenTestConfig config);
   }
   ```

2. **Client Credential Management**
   - AWS credentials provided by client (never stored in Primus)
   - Support for IAM roles, access keys, SSO
   - Credential validation and permission checks

3. **Advanced Features**
   - Multi-step attack scenario execution
   - ML-powered threat detection
   - Context-aware remediation suggestions

4. **Cost Management**
   - Estimate AWS Security Agent costs for client
   - Usage tracking and billing transparency
   - Budget alerts

### Phase 3: Advanced Security Features

**Timeline**: 8-12 weeks (after Phase 2)

**Deliverables**:

1. **Threat Modeling**
   - Automated STRIDE analysis
   - Attack tree generation
   - Risk scoring and prioritization

2. **Security Testing Framework**
   - DAST (Dynamic Application Security Testing)
   - IAST (Interactive Application Security Testing)
   - API security testing

3. **Compliance Automation**
   - SOC2 control validation
   - HIPAA safeguard checks
   - GDPR data flow analysis

4. **Security Analytics**
   - Trend analysis (vulnerability over time)
   - Team security scorecard
   - Benchmarking against industry standards

---

## 7. Business Value Analysis

### For Primus SaaS Platform

| Metric | Impact | Notes |
|--------|--------|-------|
| **Module Portfolio** | +25% | 4th major module (Identity, Logging, Notifications, Security) |
| **Market Differentiation** | High | First dev platform with integrated security-as-code |
| **Total Addressable Market** | +40% | Security is universal need vs. niche auth/logging |
| **Average Revenue Per User** | +30% | Security premium tier pricing |
| **Customer Retention** | +20% | Security is sticky; high switching cost |
| **Enterprise Sales** | +50% | Security compliance is C-level concern |

### For Primus Clients

| Benefit | Before | After | Savings |
|---------|--------|-------|---------|
| **Security Review Time** | 3 days | 2 hours | **92% faster** |
| **Cost of Breach** | $50K avg | $0 (prevention) | **100% ROI** |
| **Compliance Audit Prep** | 2 months | 2 weeks | **75% faster** |
| **Security Team Cost** | $150K/year | $0 (automation) | **$150K saved** |
| **Pen Test Frequency** | Yearly | On-demand | **365x more frequent** |

### Pricing Strategy

Following Primus module patterns, add security tier:

```text
Primus SaaS Platform Pricing

FREE TIER
• Identity Validator: ✅
• Logging: ✅  
• Notifications: ✅
• Security: ⚠️ Basic (local scanning only)

PRO TIER ($99/month per app)
• All free features
• Security: Advanced policies + compliance reports

ENTERPRISE TIER ($499/month per app)
• All pro features
• Security: AWS Security Agent integration
• Priority support
• Custom security policies
• Dedicated security engineer review
```

**Revenue Projection** (Year 1):
- 100 free users → 0
- 50 pro users → $59,400
- 20 enterprise users → $119,760
- **Total**: $179,160 ARR from security module alone

---

## 8. Competitive Advantages

### Primus SaaS Security vs. Alternatives

| Feature | Primus Security | Snyk | SonarQube | AWS Security Agent (Direct) |
|---------|----------------|------|-----------|----------------------------|
| **Integration Time** | 5 minutes | 30 minutes | 2 hours | 1 day |
| **Code Required** | 3 lines | 20+ lines | Config files | AWS setup |
| **Local Execution** | ✅ Yes | ❌ SaaS only | ✅ Yes | ❌ AWS only |
| **Zero Runtime Dependency** | ✅ Yes | ❌ No | ✅ Yes | ❌ No |
| **Multi-Language Support** | .NET, Node | All | All | AWS languages |
| **Cost (1000 scans/month)** | $99 | $299 | $0 (OSS) / $250+ | AWS pay-per-use |
| **Primus Ecosystem Integration** | ✅ Native | ❌ No | ❌ No | ❌ No |
| **Compliance Reporting** | ✅ Built-in | ✅ Yes | ⚠️ Manual | ✅ Yes |
| **Pen Testing** | ✅ Via AWS | ❌ No | ❌ No | ✅ Yes |

### Unique Selling Propositions

1. **Unified Developer Platform**
   - "One SDK for Auth, Logging, Notifications, AND Security"
   - Reduce vendor fragmentation

2. **Privacy-First Security**
   - Local scanning maintains zero PII storage
   - Optional cloud features (client controls)

3. **Developer-Friendly Integration**
   - Same DI pattern as other Primus modules
   - Familiar configuration style

4. **Best-of-Both-Worlds**
   - Fast local scanning for instant feedback
   - Advanced AWS features for deep analysis

5. **Compliance Made Easy**
   - Pre-built policy templates (OWASP, PCI-DSS, HIPAA, SOC2)
   - One-click compliance reports

---

## 9. Implementation Roadmap

### Q1 2026: Foundation

**Milestone 1: Security Module MVP (Local)**

- Week 1-2: Architecture design and SDK scaffolding
- Week 3-6: Core analyzers (SQL injection, XSS, secrets)
- Week 7-8: Dependency scanning integration
- Week 9-10: Policy engine and templates
- Week 11-12: Portal integration and documentation

**Deliverables**:
- ✅ `PrimusSaaS.Security` 1.0.0 (NuGet)
- ✅ `@primus-saas/security` 1.0.0 (NPM)
- ✅ CLI tool for CI/CD integration
- ✅ 5 pre-built security policies
- ✅ Integration guide and examples

### Q2 2026: AWS Integration

**Milestone 2: Cloud Security Features**

- Week 1-2: AWS Security Agent connector design
- Week 3-4: Credential management and IAM setup
- Week 5-6: Design review automation
- Week 7-8: Penetration testing scheduling
- Week 9-10: Cost estimation and budgeting
- Week 11-12: Beta testing with 5 enterprise clients

**Deliverables**:
- ✅ AWS connector in SDK 1.1.0
- ✅ Portal: AWS credential configuration UI
- ✅ Automated pen test scheduling
- ✅ Cost management dashboard

### Q3 2026: Advanced Features

**Milestone 3: Enterprise Security Suite**

- Week 1-4: Threat modeling automation
- Week 5-8: DAST/IAST integration
- Week 9-12: Compliance framework expansion (SOC2, HIPAA, GDPR)

**Deliverables**:
- ✅ SDK 1.2.0 with threat modeling
- ✅ Security testing framework
- ✅ 15+ compliance templates
- ✅ Security analytics dashboard

### Q4 2026: Scale & Optimize

**Milestone 4: Performance & Enterprise Features**

- Month 1: Performance optimization (large codebases)
- Month 2: Multi-repository scanning
- Month 3: Security team collaboration features

**Deliverables**:
- ✅ SDK 1.3.0 with performance improvements
- ✅ Repository-wide security posture view
- ✅ Team scorecards and benchmarking
- ✅ 50+ enterprise customers onboarded

---

## 10. Risks and Mitigation

### Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **AWS Service Changes** | Medium | High | Abstract AWS integration behind interface; easy to swap providers |
| **False Positive Rate** | High | Medium | Extensive testing; ML tuning; user feedback loop |
| **Performance (Large Codebases)** | Medium | Medium | Incremental scanning; caching; parallel processing |
| **Language Support Gaps** | Low | Low | Start with .NET/Node (core Primus stack); expand iteratively |

### Business Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Market Competition** | High | Medium | Fast execution; unique integration; pricing advantage |
| **Client AWS Costs** | Medium | Medium | Transparent cost estimation; budget controls; local-first approach |
| **Adoption Resistance** | Low | High | Free tier; proven Primus brand; clear ROI demos |
| **Compliance Complexity** | Medium | High | Partner with compliance experts; legal review; insurance |

### Compliance & Legal Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Liability for Missed Vulnerabilities** | Low | Critical | Clear ToS: "Tool assists, not guarantees"; insurance; legal review |
| **Data Privacy (Code Scanning)** | Low | High | Local-first architecture; explicit client consent for AWS features |
| **Export Control (Security Tools)** | Low | Medium | Legal counsel; jurisdiction compliance; export controls |

---

## Conclusion

### Summary

The AWS Security Agent represents an **exceptional opportunity** for Primus SaaS Platform to:

1. ✅ **Expand module portfolio** with a universally needed capability
2. ✅ **Maintain architectural principles** via hybrid local/cloud approach
3. ✅ **Differentiate in market** as first integrated dev security platform
4. ✅ **Increase revenue** through premium tier security features
5. ✅ **Improve client outcomes** with measurable security improvements

### Recommendation

**PROCEED** with Security Module development following this phased approach:

**Phase 1 (Q1 2026)**: Local security scanning MVP  
**Phase 2 (Q2 2026)**: AWS Security Agent integration  
**Phase 3 (Q3 2026)**: Advanced enterprise features

### Expected Outcomes

**Year 1 (2026)**:
- 200 security module installations
- $180K ARR from security features
- 3 enterprise case studies
- Market recognition as "most complete dev platform"

**Year 2 (2027)**:
- 1,000+ security module installations  
- $750K ARR from security features
- Industry awards for innovation
- 50% of new customers cite security as primary decision factor

### Next Steps

1. **Week 1**: Finalize technical architecture with engineering team
2. **Week 2**: Create detailed PRD (Product Requirements Document)
3. **Week 3**: Set up development environment and AWS sandbox
4. **Week 4**: Begin MVP development (Phase 1)
5. **Week 8**: Alpha testing with internal projects
6. **Week 12**: Beta release to 10 design partners

---

## Appendix A: Example Security Policies

### Policy: OWASP Top 10 Validation

```yaml
# security-policies/owasp-top-10.yml
name: OWASP Top 10 Security Policy
version: 1.0
description: Validates code against OWASP Top 10 vulnerabilities

rules:
  - id: A01-Broken-Access-Control
    severity: CRITICAL
    checks:
      - type: authorization-bypass
      - type: insecure-direct-object-reference
      - type: privilege-escalation
    action: BLOCK_DEPLOYMENT
    
  - id: A02-Cryptographic-Failures
    severity: CRITICAL
    checks:
      - type: weak-encryption
      - type: hardcoded-secrets
      - type: insecure-random
    action: BLOCK_DEPLOYMENT
    
  - id: A03-Injection
    severity: CRITICAL
    checks:
      - type: sql-injection
      - type: nosql-injection
      - type: command-injection
      - type: ldap-injection
    action: BLOCK_DEPLOYMENT

  # ... (A04-A10 omitted for brevity)
```

### Policy: PCI-DSS Compliance

```yaml
# security-policies/pci-dss.yml
name: PCI-DSS Compliance Policy
version: 3.2.1
description: Payment Card Industry Data Security Standard

requirements:
  - id: REQ-3
    title: Protect stored cardholder data
    checks:
      - type: data-encryption-at-rest
        required: true
      - type: no-plaintext-card-numbers
        pattern: '\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b'
        action: REJECT
      - type: tokenization-enforced
        
  - id: REQ-4
    title: Encrypt transmission of cardholder data
    checks:
      - type: tls-version
        minimum: 1.2
      - type: no-http-for-payments
        
  - id: REQ-6.5
    title: Develop secure applications
    checks:
      - type: input-validation
      - type: output-encoding
      - type: authentication-security
      - type: session-management
```

---

## Appendix B: Integration Code Examples

### Example 1: ASP.NET Core Integration

```csharp
// Program.cs - Full Security Integration
using PrimusSaaS.Security;

var builder = WebApplication.CreateBuilder(args);

// Add Primus Security
builder.Services.AddPrimusSecurity(options =>
{
    // Local security features (runs in-process)
    options.StaticAnalysis.Enabled = true;
    options.StaticAnalysis.Rules = new[]
    {
        SecurityRule.SqlInjection,
        SecurityRule.XssVulnerability,
        SecurityRule.HardcodedSecrets,
        SecurityRule.InsecureDeserialization
    };
    
    // Dependency scanning
    options.DependencyScanning.Enabled = true;
    options.DependencyScanning.AlertOnCritical = true;
    options.DependencyScanning.BlockOnCritical = false; // Warn only
    
    // Security policies
    options.Policies.LoadFromDirectory("./SecurityPolicies");
    options.Policies.Enforce = true;
    options.Policies.ComplianceStandards = new[]
    {
        ComplianceStandard.OWASP_Top10,
        ComplianceStandard.PCI_DSS,
        ComplianceStandard.SOC2_Type2
    };
    
    // Integration with other Primus modules
    options.Logging.Enabled = true;
    options.Logging.LogLevel = SecurityLogLevel.Warning;
    
    options.Notifications.Enabled = true;
    options.Notifications.OnCriticalVulnerability = async (vuln) =>
    {
        await notificationService.SendAsync(
            new SecurityAlertNotification(vuln)
        );
    };
    
    // Optional: AWS Security Agent (requires client AWS credentials)
    if (builder.Configuration.GetValue<bool>("Security:UseAwsAgent"))
    {
        options.AwsSecurityAgent.Region = "us-east-1";
        options.AwsSecurityAgent.PenetrationTesting = new()
        {
            Enabled = true,
            Schedule = CronExpression.Weekly(DayOfWeek.Sunday, hour: 2),
            TargetUrls = new[]
            {
                "https://api.myapp.com",
                "https://admin.myapp.com"
            },
            OnComplete = async (result) =>
            {
                // Email pen test report to security team
                await emailService.SendAsync(new PenTestReportEmail(result));
            }
        };
    }
});

var app = builder.Build();

// Security middleware (before authentication)
app.UsePrimusSecurity();

// Rest of middleware pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### Example 2: Request-Level Security Scanning

```csharp
// Controllers/PaymentController.cs
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    [HttpPost]
    [PrimusSecurityScan(
        Rules = new[] { 
            SecurityRule.SqlInjection, 
            SecurityRule.XssVulnerability,
            SecurityRule.CreditCardLeakage 
        },
        BlockOnThreat = true
    )]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] PaymentRequest request)
    {
        // Security middleware has already:
        // 1. Scanned request body for injection attempts
        // 2. Validated no credit card numbers in plain text
        // 3. Checked for XSS patterns
        // 4. Logged security event
        // 5. Blocked request if threat detected
        
        var result = await _paymentService.ProcessAsync(request);
        return Ok(result);
    }
}
```

### Example 3: Node.js/Express Integration

```typescript
// app.ts
import express from 'express';
import { 
  primusSecurityMiddleware, 
  SecurityPolicy,
  ComplianceStandard 
} from '@primus-saas/security';

const app = express();

// Primus Security Configuration
const securityConfig = {
  // Local scanning
  staticAnalysis: {
    enabled: true,
    rules: [
      'sql-injection',
      'xss-vulnerability',
      'hardcoded-secrets',
      'insecure-deserialization'
    ]
  },
  
  // Dependency scanning
  dependencyScanning: {
    enabled: true,
    alertOnCritical: true,
    blockOnCritical: false
  },
  
  // Policies
  policies: {
    directory: './security-policies',
    enforce: true,
    standards: [
      ComplianceStandard.OWASP_TOP10,
      ComplianceStandard.CIS_CRITICAL_CONTROLS
    ]
  },
  
  // Integration with Primus modules
  logging: {
    enabled: true,
    level: 'warn'
  },
  
  notifications: {
    enabled: true,
    onCriticalVulnerability: async (vuln) => {
      await notificationService.send(
        new SecurityAlertNotification(vuln)
      );
    }
  },
  
  // Optional: AWS Security Agent
  awsSecurityAgent: process.env.USE_AWS_AGENT === 'true' ? {
    region: 'us-east-1',
    penetrationTesting: {
      enabled: true,
      schedule: '0 2 * * 0', // Every Sunday at 2 AM
      targetUrls: [
        'https://api.myapp.com',
        'https://admin.myapp.com'
      ]
    }
  } : undefined
};

// Apply security middleware
app.use(primusSecurityMiddleware(securityConfig));

// Routes
app.post('/api/payment', async (req, res) => {
  // Request automatically scanned for security threats
  const result = await paymentService.process(req.body);
  res.json(result);
});

app.listen(3000);
```

---

## Appendix C: ROI Calculator

### Security Module ROI Example

**Client Profile**: Mid-size SaaS company, 20 developers

| Cost Item | Annual Cost |
|-----------|-------------|
| **Before Primus Security** | |
| Dedicated security engineer (0.5 FTE) | $75,000 |
| Third-party pen testing (4x/year) | $40,000 |
| Code review tools (Snyk + SonarQube) | $15,000 |
| Compliance audit support | $25,000 |
| **Total Before** | **$155,000** |
| | |
| **After Primus Security** | |
| Primus Security Module (Pro tier, 5 apps) | $5,940 |
| Reduced security engineer time (0.1 FTE) | $15,000 |
| AWS Security Agent costs (optional) | $3,000 |
| **Total After** | **$23,940** |
| | |
| **Annual Savings** | **$131,060** |
| **ROI** | **548%** |

---

**Document End**

*For questions or feedback on this analysis, contact: architecture@primussaas.com*
