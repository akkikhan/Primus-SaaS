# Primus SaaS Video — Complete Script + Image Package

**Purpose:** Production-ready scripts with exact image descriptions for Google Vids  
**Method:** Copy each scene → Paste script → Generate/upload matching visual

---

## 🎨 IMAGE GENERATION OPTIONS

### Option 1: Carbon.now.sh (Code Screenshots)
→ Paste code → Download PNG → Upload to Google Vids

### Option 2: Google Vids AI Image Generation  
→ Click "Stock" → "Generate image" → Use my prompts below

### Option 3: Canva / Midjourney / DALL-E
→ Use my detailed prompts for each scene

---

# SCENE-BY-SCENE SCRIPTS + IMAGES

---

## **SCENE 1: The Hook**
**Characters: 493**

### Script (Copy This):
```
Your development team just spent three months building authentication from scratch. Another two months on logging infrastructure. And now you're facing a deadline for email notifications that keeps slipping.

Sound familiar?

What if these common backend challenges could become one-line integrations instead of month-long projects?

Welcome to Primus SaaS — the developer SDK platform that's changing how modern applications are built.
```

### Image Option A — AI Generated:
**Prompt:** "Frustrated software developer at desk with multiple monitors showing complex code, clock showing late hour, deadline calendar in background, modern office, cinematic lighting, professional"

### Image Option B — Stock:
Search: "developer working late" or "software deadline stress"

---

## **SCENE 2: Platform Overview**
**Characters: 489**

### Script (Copy This):
```
Primus SaaS provides battle-tested backend modules as NuGet and npm packages. Think of it as your backend infrastructure library — authentication, logging, notifications, and more — all ready to drop into your application.

Here's what makes Primus different: Our modules run entirely inside YOUR application. No external API calls. No cloud services processing your data. Your data never leaves your infrastructure.
```

### Image Option A — AI Generated:
**Prompt:** "Modern software architecture diagram showing NuGet and npm package icons flowing into a secure application box, clean minimalist design, blue and white color scheme, tech illustration style"

### Image Option B — Code Screenshot (Carbon.now.sh):
```bash
# Install Primus packages
dotnet add package PrimusSaaS.Identity.Validator
dotnet add package PrimusSaaS.Logging
dotnet add package PrimusSaaS.Notifications
```
**Carbon Settings:** Theme: Dracula, Language: Shell, Background: #1a1a2e

---

## **SCENE 3: Privacy & Security**
**Characters: 487**

### Script (Copy This):
```
We never store or process your end-user data, tokens, or personal information. Ever. This makes compliance with GDPR, HIPAA, and other regulations dramatically simpler.

Whether your team builds with dot NET or Node JS, we've got you covered with native packages optimized for each ecosystem.

Let me show you what's available, starting with the Identity Validator module.
```

### Image Option A — AI Generated:
**Prompt:** "Data privacy shield icon with GDPR and HIPAA compliance badges, secure data flow diagram, green checkmarks, modern tech security illustration, clean white background"

### Image Option B — Diagram:
**Create in Canva/Figma:**
```
┌─────────────────────────────────────┐
│         YOUR APPLICATION            │
│  ┌─────────────────────────────┐   │
│  │     Primus SDK Modules      │   │
│  │  ✓ Data stays in-process    │   │
│  │  ✓ No external API calls    │   │
│  │  ✓ Zero PII storage         │   │
│  └─────────────────────────────┘   │
│         🔒 SECURE BOUNDARY          │
└─────────────────────────────────────┘
```

---

## **SCENE 4: Identity — The Problem**
**Characters: 485**

### Script (Copy This):
```
Building authentication traditionally means writing hundreds of lines of JWT validation code, implementing key caching, handling security vulnerabilities, and supporting multiple identity providers like Azure AD and Auth0.

This typically takes two to three months of development time.

With Primus Identity Validator, your entire authentication setup is just one line of code. That's it. Authentication done.
```

### Image — Code Screenshot (Carbon.now.sh):
**BEFORE (Traditional) — Show this mess:**
```csharp
// Traditional JWT validation - 100+ lines per issuer!
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://tenant.auth0.com/";
        options.Audience = "https://my-api";
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });
// + JWKS caching, error handling, multi-issuer support...
// + 200 more lines for each additional provider!
```
**Carbon Settings:** Theme: One Dark, Language: C#, Background: #FF4444 (red = bad)

---

## **SCENE 5: Identity — The Solution**
**Characters: 497**

### Script (Copy This):
```
What do you get with that one line? Multi-issuer JWT validation supporting Azure AD, Auth0, Okta, or your own local tokens — all simultaneously in the same application.

Automatic public key caching. Comprehensive security validation including signature verification, issuer checks, audience validation, and algorithm protection.

Performance? Under one millisecond for cached tokens.
```

### Image — Code Screenshot (Carbon.now.sh):
**AFTER (Primus) — Clean and simple:**
```csharp
// Program.cs - That's it. ONE LINE!
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

// Middleware - Standard ASP.NET
app.UseAuthentication();
app.UseAuthorization();

// Optional: Diagnostics endpoint
app.MapPrimusIdentityDiagnostics();
```
**Carbon Settings:** Theme: Dracula, Language: C#, Background: #00C853 (green = good)

---

## **SCENE 6: Identity — Configuration**
**Characters: 498**

### Script (Copy This):
```
Configuration is declarative and clean. Define your identity providers in app settings dot json. Azure AD, Auth0, local JWT — configure them all in one place. No code changes needed to add new providers.

Each issuer can have different audiences, grant types, and security settings. The SDK handles all the complexity for you automatically.
```

### Image — Code Screenshot (Carbon.now.sh):
```json
{
  "PrimusIdentity": {
    "Issuers": [
      {
        "Name": "AzureAD",
        "Type": "Oidc",
        "Authority": "https://login.microsoftonline.com/{tenant}/v2.0",
        "Audiences": ["api://your-app-id"]
      },
      {
        "Name": "Auth0",
        "Type": "Oidc", 
        "Authority": "https://your-tenant.auth0.com/",
        "Audiences": ["https://your-api"],
        "AllowMachineToMachine": true
      },
      {
        "Name": "LocalDev",
        "Type": "Jwt",
        "Issuer": "https://localhost:5001",
        "Secret": "your-dev-signing-key"
      }
    ]
  }
}
```
**Carbon Settings:** Theme: Night Owl, Language: JSON, Background: #2D3748

---

## **SCENE 7: Logging — The Problem**
**Characters: 492**

### Script (Copy This):
```
Next up — the Logging module.

Setting up proper logging infrastructure traditionally means configuring multiple providers, building JSON formatters, implementing PII redaction, and setting up file rotation. That's easily a month of dedicated work.

With Primus Logging, one line gives you enterprise-grade logging with structured JSON output, multiple targets, and automatic PII masking.
```

### Image Option A — AI Generated:
**Prompt:** "Complex logging infrastructure diagram with multiple arrows, configuration files, and warning symbols, showing overwhelming complexity, dark red accents indicating problems"

### Image Option B — Code Screenshot:
**BEFORE (Traditional logging setup):**
```csharp
// Traditional logging setup - weeks of configuration
builder.Logging.AddConsole();
builder.Logging.AddDebug();
// + Serilog/NLog configuration
// + Custom JSON formatters
// + PII masking implementation
// + File rotation logic
// + Async buffering
// + Azure Application Insights setup
// + Correlation ID middleware
// + HTTP context enrichment
// ... 500+ lines of infrastructure code
```
**Carbon Settings:** Theme: Monokai, Language: C#, Background: #FF6B6B

---

## **SCENE 8: Logging — The Solution**
**Characters: 496**

### Script (Copy This):
```
With Primus Logging, your entire setup looks like this. Clear providers, add Primus, bind configuration. Three lines total.

You get structured JSON logging, automatic PII redaction, console and file output, Azure Application Insights integration, async buffering, and request correlation — all from configuration.
```

### Image — Code Screenshot (Carbon.now.sh):
```csharp
// Program.cs - Enterprise logging in 3 lines
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    builder.Configuration.GetSection("PrimusLogging").Bind(options);
});

// Add middleware for HTTP context enrichment
app.UsePrimusLogging();
```
**Carbon Settings:** Theme: Dracula, Language: C#, Background: #00C853

---

## **SCENE 9: Logging — PII Masking**
**Characters: 499**

### Script (Copy This):
```
Here's the game-changer — automatic PII masking. Email addresses, credit card numbers, and social security numbers are automatically redacted before they hit your logs.

No manual configuration needed. No compliance headaches. Your logs stay clean, your auditors stay happy, and sensitive data never leaks.
```

### Image — Side-by-side comparison:
**Create two Carbon screenshots side by side:**

**LEFT (Before Masking):**
```json
{
  "message": "User john.doe@company.com logged in",
  "context": {
    "email": "john.doe@company.com",
    "creditCard": "4532-1234-5678-9012",
    "ssn": "123-45-6789"
  }
}
```

**RIGHT (After Masking):**
```json
{
  "message": "User ***REDACTED*** logged in",
  "context": {
    "email": "***REDACTED***",
    "creditCard": "***REDACTED***",
    "ssn": "***REDACTED***"
  }
}
```
**Carbon Settings:** Left: Red background, Right: Green background

---

## **SCENE 10: Notifications — Intro**
**Characters: 491**

### Script (Copy This):
```
The Notifications module transforms how you handle user communications.

Traditional notification systems require SMTP setup, SMS provider integration, HTML templates hardcoded in your application, and complex retry logic. Weeks of work.

With Primus Notifications, configure your SMTP, point to your templates folder, and you're done.
```

### Image — Code Screenshot (Carbon.now.sh):
```csharp
// Program.cs - Multi-channel notifications
builder.Services.AddPrimusNotifications(notifications =>
{
    // Email via SMTP
    notifications.UseSmtp(builder.Configuration.GetSection("Smtp"));
    
    // Templates from file system
    notifications.UseFileTemplates("NotificationTemplates",
        validateOnStartup: true,
        watchForChanges: true);
    
    // Built-in queue with retry logic
    notifications.UseInMemoryQueue(o =>
    {
        o.BoundedCapacity = 500;
        o.MaxParallelHandlers = 2;
    });
});
```
**Carbon Settings:** Theme: Dracula, Language: C#, Background: #6C5CE7

---

## **SCENE 11: Notifications — Templates**
**Characters: 496**

### Script (Copy This):
```
The magic is in Liquid templating. Store your email and SMS templates as simple files. Edit them without recompiling your application.

Your marketing team can update email copy without involving developers. Just save the file and changes are live. No deployments required.
```

### Image — File structure + template:
```
NotificationTemplates/
├── PasswordReset/
│   ├── EmailSubject.liquid
│   ├── EmailBody.liquid
│   └── SmsBody.liquid
├── WelcomeEmail/
│   ├── EmailSubject.liquid
│   └── EmailBody.liquid
└── Partials/
    ├── email_header.liquid
    └── email_footer.liquid
```

**Plus template content:**
```liquid
{% include 'Partials/email_header' %}

Hi {{ recipient.name | default: "there" }},

Your password reset code is: {{ data.code }}

This code expires in {{ data.expiryMinutes }} minutes.

{% include 'Partials/email_footer' %}
```

---

## **SCENE 12: Coming Soon**
**Characters: 489**

### Script (Copy This):
```
We're not stopping here. Coming soon — the Feature Flags module, already in preview.

Control feature rollouts with precision. Release to five percent of users, then twenty-five, then everyone. Target specific users or groups. Schedule activation windows.

Also in preview — Document Renderer for professional PDF generation.
```

### Image — Code Screenshot:
```csharp
// Feature Flags - Controlled rollouts
if (featureFlags.IsEnabled("NewDashboard", userId))
{
    // Show new dashboard to selected users
}

// Percentage-based rollout
var config = new FeatureFlagConfig
{
    Name = "NewCheckout",
    RolloutPercentage = 25,  // 25% of users
    TargetGroups = ["beta-testers", "premium"],
    TimeWindow = new TimeWindow(start, end)
};
```
**Carbon Settings:** Theme: Night Owl, Language: C#, Background: #9B59B6

---

## **SCENE 13: Business Impact**
**Characters: 497**

### Script (Copy This):
```
Let's talk business impact.

Authentication that takes three months? Done in a day with Primus. Logging infrastructure that takes a month? An afternoon. Notification systems that take weeks? Hours.

Teams reduce infrastructure development time by seventy to eighty percent. Security vulnerabilities in custom code are common and costly — our modules are battle-tested.
```

### Image — Comparison chart:
**Create in Canva or use AI:**

| Task | Traditional | With Primus | Savings |
|------|-------------|-------------|---------|
| Authentication | 3 months | 1 day | 98% |
| Logging | 1 month | 3 hours | 99% |
| Notifications | 2 weeks | 2 hours | 99% |
| **Total** | **4.5 months** | **2 days** | **97%** |

**AI Prompt:** "Professional business comparison chart showing dramatic time savings, blue and green colors, clean corporate style, infographic"

---

## **SCENE 14: Getting Started**
**Characters: 494**

### Script (Copy This):
```
Your developers want to build features that matter, not reinvent authentication for the hundredth time. Primus lets them focus on what makes your application unique.

Getting started is simple. Install the packages with dotnet add package. Add your configuration. Wire up the services. That's it — you're ready to ship.
```

### Image — Terminal screenshot:
```powershell
# Get started in 5 minutes

# Step 1: Install packages
dotnet add package PrimusSaaS.Identity.Validator
dotnet add package PrimusSaaS.Logging
dotnet add package PrimusSaaS.Notifications

# Step 2: Run your app
dotnet run

# Step 3: Test authentication
curl -H "Authorization: Bearer <token>" https://localhost:5001/whoami

# ✅ You're done! Ship features, not infrastructure.
```
**Carbon Settings:** Theme: Cobalt, Language: PowerShell, Background: #1E3A5F

---

## **SCENE 15: Closing**
**Characters: 332**

### Script (Copy This):
```
Primus SaaS — production-ready modules, zero vendor lock-in, minutes to integrate.

Stop building infrastructure. Start building your product.

Visit our documentation to explore the platform and try our live demo today.

Thank you for watching.
```

### Image Option A — AI Generated:
**Prompt:** "Modern tech company logo reveal, 'Primus SaaS' text, rocket ship launching symbolizing product launch, clean gradient background blue to purple, professional"

### Image Option B — Branded slide:
```
┌─────────────────────────────────────────┐
│                                         │
│           🚀 PRIMUS SAAS                │
│                                         │
│    Production-Ready Backend Modules     │
│                                         │
│    ✓ Zero Vendor Lock-in               │
│    ✓ Minutes to Integrate              │
│    ✓ Enterprise Security               │
│                                         │
│    📚 docs.primus-saas.com             │
│    🎮 demo.primus-saas.com             │
│                                         │
└─────────────────────────────────────────┘
```

---

# 🎬 QUICK CARBON.NOW.SH WORKFLOW

1. Go to **carbon.now.sh**
2. Paste code from each scene
3. Settings:
   - **Theme:** Dracula (good) or One Dark (problems)
   - **Language:** Match the code (C#, JSON, PowerShell)
   - **Background:** Green (#00C853) for "After/Good", Red (#FF4444) for "Before/Bad"
   - **Window Controls:** None
   - **Width:** 680px (fits Google Vids well)
4. Click **Export** → PNG
5. Upload to Google Vids scene

---

# 📋 SCENE SUMMARY

| Scene | Topic | Script Chars | Visual Type |
|-------|-------|--------------|-------------|
| 1 | Hook | 493 | AI Image: frustrated dev |
| 2 | Platform Overview | 489 | Code: package install |
| 3 | Privacy | 487 | Diagram: secure boundary |
| 4 | Identity Problem | 485 | Code: traditional mess (RED) |
| 5 | Identity Solution | 497 | Code: one-line setup (GREEN) |
| 6 | Identity Config | 498 | Code: appsettings.json |
| 7 | Logging Problem | 492 | Code: complex setup (RED) |
| 8 | Logging Solution | 496 | Code: 3-line setup (GREEN) |
| 9 | PII Masking | 499 | Side-by-side: before/after |
| 10 | Notifications | 491 | Code: AddPrimusNotifications |
| 11 | Templates | 496 | File structure + Liquid |
| 12 | Coming Soon | 489 | Code: Feature Flags |
| 13 | Business Impact | 497 | Chart: time savings |
| 14 | Getting Started | 494 | Terminal: commands |
| 15 | Closing | 332 | Logo/branded slide |

**Total: 15 scenes, ~7-8 minutes runtime**

---

*Generated from Primus SaaS documentation*  
*Ready for Google Vids production*
