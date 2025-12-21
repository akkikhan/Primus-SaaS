# 🔄 Primus SaaS - Client App Flow Diagrams

**Version**: 1.0  
**Date**: November 24, 2025  
**Purpose**: Step-by-step flow diagrams for each module integration from client perspective

---

## 📋 Table of Contents

1. [Module 1: Identity Validator - Complete Flow](#module-1-identity-validator)
2. [Module 2: Logging - Complete Flow](#module-2-logging)
3. [Cross-Module Integration Flow](#cross-module-integration)
4. [Benefits Summary](#benefits-summary)

---

## Module 1: Identity Validator - Complete Flow

### 1.1 Initial Setup Flow

```mermaid
flowchart TD
    Start([Client Developer Starts]) --> A1[Receives Email from Primus Admin]
    A1 --> A2[Email Contains:<br/>- Application ID<br/>- Client Secret<br/>- Documentation Link]
    
    A2 --> B1{Choose Technology Stack}
    B1 -->|.NET| C1[Install NuGet Package]
    B1 -->|Node.js| C2[Install NPM Package]
    
    C1 --> D1[dotnet add package<br/>PrimusSaaS.Identity.Validator]
    C2 --> D2[npm install<br/>primus-identity-validator]
    
    D1 --> E1[Configure appsettings.json]
    D2 --> E2[Configure in code]
    
    E1 --> F1[Add to Program.cs:<br/>- AddPrimusIdentityValidator<br/>- UsePrimusIdentityValidator]
    E2 --> F2[Add middleware:<br/>- Import validator<br/>- Configure options<br/>- Use middleware]
    
    F1 --> G[Test with Sample Token]
    F2 --> G
    
    G --> H{Test Successful?}
    H -->|Yes| I[✅ Integration Complete]
    H -->|No| J[Check Error Reference Guide]
    J --> K[Fix Configuration]
    K --> G
    
    I --> L[Deploy to Production]
    
    style Start fill:#e1f5e1
    style I fill:#c8e6c9
    style L fill:#81c784
```

**Benefits at Each Step:**
- **Email Receipt**: Zero manual configuration lookup - everything provided upfront
- **Package Install**: Single command - no complex dependencies
- **Configuration**: Pre-filled values from portal - copy-paste ready
- **Testing**: Immediate feedback - know if it works before deployment
- **Error Handling**: Guided troubleshooting - faster resolution

---

### 1.2 Authentication Mode Selection Flow

```mermaid
flowchart TD
    Start([Choose Auth Mode]) --> A{What's Your Identity Provider?}
    
    A -->|Azure AD| B1[Mode: AzureAd]
    A -->|Local JWT| B2[Mode: Local]
    A -->|Both| B3[Mode: Hybrid]
    
    B1 --> C1[Configure:<br/>- TenantId<br/>- Audience<br/>- Authority]
    B2 --> C2[Configure:<br/>- JwtSecret<br/>- Issuer<br/>- Audience]
    B3 --> C3[Configure Both:<br/>- Azure AD settings<br/>- Local settings<br/>- Fallback order]
    
    C1 --> D1[SDK fetches JWKS<br/>from Azure AD]
    C2 --> D2[SDK uses symmetric<br/>key validation]
    C3 --> D3[SDK tries Azure first,<br/>falls back to Local]
    
    D1 --> E1[Validates RS256 tokens]
    D2 --> E2[Validates HS256 tokens]
    D3 --> E3[Validates both types]
    
    E1 --> F[✅ Tokens Validated]
    E2 --> F
    E3 --> F
    
    F --> G[User info available<br/>in req.primusUser]
    
    style Start fill:#e3f2fd
    style F fill:#90caf9
    style G fill:#42a5f5
```

**Benefits:**
- **Flexibility**: Support any identity provider without code changes
- **Migration Path**: Hybrid mode enables gradual migration from Local to Azure AD
- **Zero Vendor Lock-in**: Switch providers by changing configuration only
- **Multi-tenant Ready**: Different tenants can use different providers

---

### 1.3 Runtime Request Flow (Azure AD Mode)

```mermaid
sequenceDiagram
    participant User as End User
    participant SPA as Frontend (SPA)
    participant AAD as Azure AD
    participant API as Client API
    participant SDK as Primus SDK
    participant Cache as JWKS Cache
    
    User->>SPA: 1. Click Login
    SPA->>AAD: 2. Redirect to Azure AD
    AAD->>User: 3. Show Login Page
    User->>AAD: 4. Enter Credentials
    AAD->>AAD: 5. Validate Credentials
    AAD->>SPA: 6. Return Access Token (JWT)
    
    Note over SPA: Token stored in memory
    
    SPA->>API: 7. GET /api/orders<br/>Authorization: Bearer {token}
    
    API->>SDK: 8. Request hits middleware
    SDK->>SDK: 9. Extract token from header
    
    SDK->>Cache: 10. Check JWKS cache
    
    alt Cache Hit
        Cache-->>SDK: 11a. Return cached keys
    else Cache Miss
        SDK->>AAD: 11b. Fetch JWKS endpoint
        AAD-->>SDK: 11c. Return public keys
        SDK->>Cache: 11d. Store in cache (24h TTL)
    end
    
    SDK->>SDK: 12. Validate token signature
    SDK->>SDK: 13. Validate issuer
    SDK->>SDK: 14. Validate audience
    SDK->>SDK: 15. Validate expiry
    SDK->>SDK: 16. Extract claims
    
    alt Valid Token
        SDK->>SDK: 17a. Attach user to request
        SDK->>API: 17b. Continue to controller
        API->>API: 18. Process business logic
        API->>SPA: 19. Return data
        SPA->>User: 20. Display results
    else Invalid Token
        SDK->>API: 17c. Return 401 Unauthorized
        API->>SPA: 18. Error response
        SPA->>User: 19. Show login prompt
    end
    
    Note over SDK,Cache: ⚡ Cached validation: <5ms<br/>🔒 Portal NOT in runtime path
```

**Benefits:**
- **Performance**: JWKS caching means <5ms validation after first request
- **Offline Capable**: No dependency on Primus Portal at runtime
- **Scalability**: Stateless validation - scales horizontally
- **Security**: Cryptographic validation - no database lookups
- **Reliability**: Works even if portal is down

---

### 1.4 Error Handling Flow

```mermaid
flowchart TD
    Start([Token Validation Fails]) --> A{What's the Error?}
    
    A -->|Invalid Signature| B1[Error: INVALID_SIGNATURE]
    A -->|Token Expired| B2[Error: TOKEN_EXPIRED]
    A -->|Wrong Audience| B3[Error: INVALID_AUDIENCE]
    A -->|Wrong Issuer| B4[Error: INVALID_ISSUER]
    A -->|Malformed Token| B5[Error: MALFORMED_TOKEN]
    
    B1 --> C1[Check:<br/>- JWKS endpoint accessible?<br/>- Token from correct tenant?<br/>- Clock skew issues?]
    B2 --> C2[Check:<br/>- Token not expired?<br/>- System time correct?<br/>- Refresh token flow working?]
    B3 --> C3[Check:<br/>- Audience matches config?<br/>- Azure AD app ID correct?<br/>- Case sensitivity?]
    B4 --> C4[Check:<br/>- Tenant ID correct?<br/>- Authority URL correct?<br/>- Multi-tenant config?]
    B5 --> C5[Check:<br/>- Token format (3 parts)?<br/>- Base64 encoding valid?<br/>- Special characters?]
    
    C1 --> D[Consult ERROR_REFERENCE.md]
    C2 --> D
    C3 --> D
    C4 --> D
    C5 --> D
    
    D --> E[Apply Fix from Guide]
    E --> F{Fixed?}
    F -->|Yes| G[✅ Validation Succeeds]
    F -->|No| H[Contact Support with:<br/>- Error code<br/>- Token header<br/>- Configuration]
    
    H --> I[Support provides solution]
    I --> E
    
    style Start fill:#ffebee
    style G fill:#c8e6c9
    style H fill:#fff9c4
```

**Benefits:**
- **Self-Service**: Comprehensive error guides reduce support tickets
- **Fast Resolution**: Common errors documented with solutions
- **Learning**: Developers understand authentication better
- **Debugging**: Clear error codes make troubleshooting systematic

---

### 1.5 Upgrade Flow (Version Update)

```mermaid
flowchart TD
    Start([New Version Released]) --> A[Portal Admin releases v2.0.0]
    
    A --> B[Portal identifies apps using v1.x]
    B --> C[Email sent to developers]
    
    C --> D[Email Contains:<br/>- Version number<br/>- Breaking changes<br/>- Migration guide<br/>- Deadline]
    
    D --> E[Developer reviews changes]
    E --> F{Breaking Changes?}
    
    F -->|Yes| G1[Read migration guide]
    F -->|No| G2[Simple version bump]
    
    G1 --> H1[Update code per guide:<br/>- Configuration changes<br/>- API signature changes<br/>- Behavior changes]
    G2 --> H2[Update package version only]
    
    H1 --> I[Test in dev environment]
    H2 --> I
    
    I --> J{Tests Pass?}
    J -->|Yes| K[Deploy to staging]
    J -->|No| L[Fix issues]
    L --> I
    
    K --> M[Verify in staging]
    M --> N{Staging OK?}
    N -->|Yes| O[Deploy to production]
    N -->|No| L
    
    O --> P[Mark as upgraded in portal]
    P --> Q[✅ Upgrade Complete]
    
    style Start fill:#e1f5e1
    style Q fill:#81c784
```

**Benefits:**
- **Proactive Notification**: Know about updates before they're critical
- **Guided Migration**: Step-by-step instructions reduce errors
- **Version Tracking**: Portal knows what version each app uses
- **Rollback Plan**: Clear documentation enables safe rollbacks
- **Minimal Downtime**: Test in lower environments first

---

## Module 2: Logging - Complete Flow

### 2.1 Initial Setup Flow

```mermaid
flowchart TD
    Start([Developer Wants Logging]) --> A[Check portal for Logging module]
    
    A --> B[Admin assigns Logging module]
    B --> C[Email with integration docs]
    
    C --> D{Choose Stack}
    D -->|.NET| E1[Install NuGet:<br/>PrimusSaaS.Logging]
    D -->|Node.js| E2[Install NPM:<br/>@primus-saas/logging]
    
    E1 --> F1[Configure in appsettings.json:<br/>- ApplicationId<br/>- Environment<br/>- MinLevel<br/>- Targets]
    E2 --> F2[Configure in code:<br/>- ApplicationId<br/>- Environment<br/>- MinLevel<br/>- Targets]
    
    F1 --> G1[Initialize logger:<br/>services.AddPrimusLogging()]
    F2 --> G2[Initialize logger:<br/>createLogger(config)]
    
    G1 --> H[Replace existing logging calls]
    G2 --> H
    
    H --> I[logger.info('message', context)]
    I --> J[Logs written to configured targets]
    
    J --> K{Verify logs}
    K -->|Console| L1[See structured JSON in console]
    K -->|File| L2[See logs in file]
    K -->|Both| L3[See in both locations]
    
    L1 --> M[✅ Logging Active]
    L2 --> M
    L3 --> M
    
    style Start fill:#fff3e0
    style M fill:#ffb74d
```

**Benefits:**
- **Quick Setup**: 5 minutes from install to first log
- **No Infrastructure**: No need to set up log servers initially
- **Structured Output**: JSON format works with any log aggregator
- **Flexible Targets**: Console for dev, files for production

---

### 2.2 Runtime Logging Flow

```mermaid
sequenceDiagram
    participant App as Application Code
    participant Logger as Primus Logger
    participant Enricher as Context Enricher
    participant Masker as PII Masker
    participant Target as Log Target
    participant File as Log File
    
    App->>Logger: 1. logger.info('User login', {userId: '123'})
    
    Logger->>Logger: 2. Check log level (INFO >= minLevel?)
    
    alt Level Enabled
        Logger->>Enricher: 3. Enrich with context
        Enricher->>Enricher: 4. Add timestamp
        Enricher->>Enricher: 5. Add requestId (from middleware)
        Enricher->>Enricher: 6. Add tenantId (from auth)
        Enricher->>Enricher: 7. Add applicationId
        Enricher->>Enricher: 8. Add environment
        
        Enricher->>Masker: 9. Check for sensitive data
        Masker->>Masker: 10. Scan for PII patterns
        
        alt Contains PII
            Masker->>Masker: 11a. Mask password fields
            Masker->>Masker: 11b. Mask SSN patterns
            Masker->>Masker: 11c. Mask credit card numbers
        end
        
        Masker->>Logger: 12. Return sanitized log entry
        
        Logger->>Target: 13. Format as JSON
        Target->>File: 14. Write to file (async)
        Target->>App: 15. Write to console (if enabled)
        
        Note over Logger,File: ⚡ Non-blocking: <1ms overhead
    else Level Disabled
        Logger->>App: Skip logging (DEBUG < INFO)
    end
```

**Benefits:**
- **Auto-Enrichment**: Context added automatically - no manual tracking
- **Security**: PII masked by default - compliance ready
- **Performance**: Async writes - no blocking
- **Correlation**: RequestId links all logs from same request
- **Multi-tenant Safe**: TenantId ensures log isolation

---

### 2.3 Integration with Identity Validator

```mermaid
flowchart TD
    Start([Request Arrives]) --> A[Identity Validator Middleware]
    
    A --> B[Validate Token]
    B --> C{Valid?}
    
    C -->|Yes| D[Extract user info:<br/>- userId<br/>- tenantId<br/>- roles]
    C -->|No| E[Logger: Authentication failed]
    
    D --> F[Attach to request context]
    F --> G[Logger middleware detects context]
    
    G --> H[Logger auto-enriches with:<br/>- userId from token<br/>- tenantId from token<br/>- requestId (generated)]
    
    H --> I[Application code executes]
    I --> J[logger.info('Processing order')]
    
    J --> K[Log includes ALL context automatically]
    
    K --> L{Log Output}
    L --> M[Console: Structured JSON]
    L --> N[File: Structured JSON]
    
    E --> O[Log includes:<br/>- Error reason<br/>- Token header<br/>- IP address]
    
    style Start fill:#e8f5e9
    style K fill:#66bb6a
    style O fill:#ef5350
```

**Benefits:**
- **Zero Configuration**: Identity + Logging work together automatically
- **Complete Context**: Every log knows who, what, when, where
- **Security Audit**: Failed auth attempts logged automatically
- **Troubleshooting**: Trace user journey across all logs
- **Compliance**: User actions tracked for audit

---

### 2.4 Distributed Tracing Flow

```mermaid
sequenceDiagram
    participant Client as Client App
    participant ServiceA as Order Service
    participant ServiceB as Payment Service
    participant ServiceC as Email Service
    participant Logs as Log Aggregator
    
    Client->>ServiceA: 1. POST /orders
    ServiceA->>ServiceA: 2. Generate correlationId: "abc-123"
    ServiceA->>ServiceA: 3. logger.info('Order created', {correlationId})
    
    ServiceA->>ServiceB: 4. POST /payments<br/>X-Correlation-ID: abc-123
    ServiceB->>ServiceB: 5. Extract correlationId from header
    ServiceB->>ServiceB: 6. logger.info('Payment processing', {correlationId})
    
    ServiceB->>ServiceC: 7. POST /emails<br/>X-Correlation-ID: abc-123
    ServiceC->>ServiceC: 8. Extract correlationId from header
    ServiceC->>ServiceC: 9. logger.info('Email sent', {correlationId})
    
    ServiceA->>Logs: 10. Log: Order created (abc-123)
    ServiceB->>Logs: 11. Log: Payment processed (abc-123)
    ServiceC->>Logs: 12. Log: Email sent (abc-123)
    
    Note over Logs: Search "abc-123" shows complete flow:<br/>1. Order created at 10:00:01<br/>2. Payment processed at 10:00:03<br/>3. Email sent at 10:00:05
    
    Logs->>Client: 13. Developer queries logs by correlationId
    
    Note over Client,Logs: ✅ Complete request journey visible
```

**Benefits:**
- **End-to-End Visibility**: See entire flow across services
- **Performance Analysis**: Identify slow services
- **Error Tracking**: Know exactly where failures occur
- **Debugging**: Reproduce issues with complete context
- **SLA Monitoring**: Measure actual user experience

---

### 2.5 Log Analysis Flow

```mermaid
flowchart TD
    Start([Logs Generated]) --> A{Where are logs?}
    
    A -->|Local Files| B1[Use grep/PowerShell]
    A -->|CloudWatch| B2[Use CloudWatch Insights]
    A -->|ELK Stack| B3[Use Kibana]
    A -->|Splunk| B4[Use Splunk Search]
    
    B1 --> C1[Search by:<br/>- correlationId<br/>- userId<br/>- level: ERROR]
    B2 --> C2[Query:<br/>fields @timestamp, message<br/>filter level = 'ERROR']
    B3 --> C3[Query:<br/>level:ERROR AND<br/>tenantId:acme-corp]
    B4 --> C4[Search:<br/>index=app level=ERROR<br/>| stats count by userId]
    
    C1 --> D[Find relevant logs]
    C2 --> D
    C3 --> D
    C4 --> D
    
    D --> E{Analysis Type}
    
    E -->|Debugging| F1[Trace correlationId<br/>through all services]
    E -->|Performance| F2[Measure time between<br/>log entries]
    E -->|Security| F3[Find failed auth attempts<br/>by userId]
    E -->|Compliance| F4[Export logs for<br/>audit period]
    
    F1 --> G[Identify root cause]
    F2 --> H[Identify bottleneck]
    F3 --> I[Identify attack pattern]
    F4 --> J[Generate audit report]
    
    style Start fill:#e1f5fe
    style G fill:#4fc3f7
    style H fill:#4fc3f7
    style I fill:#4fc3f7
    style J fill:#4fc3f7
```

**Benefits:**
- **Tool Agnostic**: Works with any log aggregator
- **Powerful Queries**: Structured JSON enables complex searches
- **Fast Troubleshooting**: Find issues in seconds, not hours
- **Compliance Ready**: Easy to generate audit reports
- **Cost Effective**: Use free tools (grep) or enterprise (Splunk)

---

## Cross-Module Integration Flow

### 3.1 Identity Validator + Logging Combined

```mermaid
flowchart TD
    Start([Request with JWT]) --> A[Identity Validator Middleware]
    
    A --> B[Logging: Auth attempt started]
    B --> C[Validate token]
    
    C --> D{Valid?}
    
    D -->|Yes| E[Extract claims:<br/>userId, tenantId, roles]
    D -->|No| F[Logging: Auth failed<br/>Reason: Invalid signature]
    
    E --> G[Attach to request.user]
    G --> H[Logging: Auth successful<br/>User: john@acme.com]
    
    H --> I[Logger Context Updated:<br/>- userId: john@acme.com<br/>- tenantId: acme-corp<br/>- roles: [Admin]]
    
    I --> J[Application Controller]
    J --> K[logger.info('Fetching orders')]
    
    K --> L[Log Output:<br/>{<br/>  timestamp: '2025-11-24T13:30:00Z',<br/>  level: 'INFO',<br/>  message: 'Fetching orders',<br/>  userId: 'john@acme.com',<br/>  tenantId: 'acme-corp',<br/>  roles: ['Admin'],<br/>  requestId: 'req-abc-123',<br/>  applicationId: 'PSP-CLI-711224'<br/>}]
    
    F --> M[Log Output:<br/>{<br/>  timestamp: '2025-11-24T13:30:00Z',<br/>  level: 'ERROR',<br/>  message: 'Auth failed',<br/>  reason: 'Invalid signature',<br/>  ipAddress: '192.168.1.100',<br/>  requestId: 'req-abc-124'<br/>}]
    
    L --> N[✅ Complete Audit Trail]
    M --> N
    
    style Start fill:#f3e5f5
    style N fill:#ab47bc
```

**Combined Benefits:**
- **Automatic Context**: Auth info flows to all logs
- **Security Audit**: Every action tied to authenticated user
- **Compliance**: Complete audit trail for regulations
- **Troubleshooting**: Know who did what when
- **Zero Effort**: No manual correlation needed

---

## Benefits Summary

### Module 1: Identity Validator Benefits

| Benefit Category | Specific Benefits |
|-----------------|-------------------|
| **Developer Experience** | • 5-minute integration<br/>• Pre-configured documentation<br/>• Copy-paste ready code<br/>• Comprehensive error guides |
| **Security** | • Cryptographic validation<br/>• No secrets in code<br/>• Multi-tenant isolation<br/>• Algorithm enforcement |
| **Performance** | • <5ms validation (cached)<br/>• Stateless (scales horizontally)<br/>• No database lookups<br/>• Offline capable |
| **Flexibility** | • Multiple auth modes<br/>• Provider agnostic<br/>• Hybrid support<br/>• Easy migration path |
| **Maintenance** | • Version tracking<br/>• Upgrade notifications<br/>• Migration guides<br/>• Portal management |

---

### Module 2: Logging Benefits

| Benefit Category | Specific Benefits |
|-----------------|-------------------|
| **Observability** | • Structured JSON logs<br/>• Auto-enriched context<br/>• Correlation IDs<br/>• Distributed tracing |
| **Security** | • PII masking<br/>• Failed auth logging<br/>• Audit trails<br/>• Compliance ready |
| **Performance** | • <1ms overhead<br/>• Async writes<br/>• Configurable levels<br/>• Minimal memory |
| **Integration** | • Works with any aggregator<br/>• Multiple targets<br/>• Auto-context from auth<br/>• Zero configuration |
| **Debugging** | • Complete request journey<br/>• Error context<br/>• Performance metrics<br/>• User action tracking |

---

### Combined Modules Benefits

```mermaid
mindmap
  root((Primus SaaS<br/>Benefits))
    Developer Experience
      5-min setup
      Pre-configured
      Self-service
      Great docs
    Security
      Crypto validation
      PII masking
      Audit trails
      Compliance
    Performance
      <5ms auth
      <1ms logging
      Scales horizontally
      No blocking
    Cost Savings
      No custom dev
      Faster debugging
      Less support
      Free tools
    Reliability
      Offline capable
      No portal dependency
      Proven patterns
      Battle tested
    Flexibility
      Any provider
      Any log tool
      Any stack
      Easy migration
```

---

## Real-World Example: E-Commerce Application

### Scenario: Order Processing Flow

```mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant OrderAPI
    participant PaymentAPI
    participant Logger
    participant Logs
    
    User->>Frontend: 1. Place Order
    Frontend->>OrderAPI: 2. POST /orders<br/>Bearer: {azure-token}
    
    Note over OrderAPI: Identity Validator
    OrderAPI->>OrderAPI: 3. Validate token
    OrderAPI->>Logger: 4. Log: Order attempt (userId: john)
    
    OrderAPI->>OrderAPI: 5. Create order
    OrderAPI->>Logger: 6. Log: Order created (orderId: 123, correlationId: abc)
    
    OrderAPI->>PaymentAPI: 7. POST /payments<br/>X-Correlation-ID: abc
    
    Note over PaymentAPI: Identity Validator
    PaymentAPI->>PaymentAPI: 8. Validate token
    PaymentAPI->>Logger: 9. Log: Payment attempt (correlationId: abc)
    
    PaymentAPI->>PaymentAPI: 10. Process payment
    PaymentAPI->>Logger: 11. Log: Payment success (correlationId: abc)
    
    PaymentAPI->>OrderAPI: 12. Payment confirmed
    OrderAPI->>Logger: 13. Log: Order completed (correlationId: abc)
    
    OrderAPI->>Frontend: 14. Success response
    Frontend->>User: 15. Show confirmation
    
    Note over Logs: Complete audit trail:<br/>• Who: john@acme.com<br/>• What: Order 123<br/>• When: 2025-11-24 13:30:00<br/>• Where: OrderAPI, PaymentAPI<br/>• How: correlationId: abc
```

**What Developer Gets:**
1. ✅ **Authentication**: Automatic token validation
2. ✅ **Authorization**: User roles available
3. ✅ **Audit Trail**: Complete log of actions
4. ✅ **Debugging**: Trace entire flow with correlationId
5. ✅ **Security**: PII masked, failed attempts logged
6. ✅ **Compliance**: Full audit for regulations

**Time Saved:**
- Without Primus: **2-3 weeks** to build auth + logging
- With Primus: **1-2 hours** to integrate both modules
- **Savings**: 95% reduction in integration time

---

## Next Steps for Clients

### For New Clients

1. **Contact Primus Admin** → Get application registered
2. **Receive Email** → Contains all credentials and docs
3. **Install Packages** → Single command per module
4. **Configure** → Copy-paste from documentation
5. **Test** → Verify in dev environment
6. **Deploy** → Push to production

### For Existing Clients

1. **Check Portal** → See available module updates
2. **Review Changes** → Read migration guides
3. **Test Upgrade** → Verify in staging
4. **Deploy** → Roll out to production
5. **Acknowledge** → Mark as upgraded in portal

---

**Document Version**: 1.0  
**Last Updated**: November 24, 2025  
**Maintained By**: Primus Platform Team  
**Feedback**: Contact support@primussaas.com
