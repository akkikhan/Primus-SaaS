# Primus SaaS Video — Ready-to-Use Carbon Links & Images

**Instructions:** Click each Carbon link → Screenshot will open → Click "Export" → Download PNG → Upload to Google Vids

---

## SCENE 2: Package Install
**Use for:** Platform Overview

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%281%2C22%2C39%2C1%29&t=dracula&wt=none&l=application%2Fx-sh&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=14px&lh=152%25&si=false&es=2x&wm=false&code=%2523%2520Install%2520Primus%2520packages%250Adotnet%2520add%2520package%2520PrimusSaaS.Identity.Validator%250Adotnet%2520add%2520package%2520PrimusSaaS.Logging%250Adotnet%2520add%2520package%2520PrimusSaaS.Notifications
```

**Or copy this code to carbon.now.sh manually:**
```bash
# Install Primus packages
dotnet add package PrimusSaaS.Identity.Validator
dotnet add package PrimusSaaS.Logging
dotnet add package PrimusSaaS.Notifications
```

---

## SCENE 4: Identity — Traditional Code (RED background)
**Use for:** Showing the problem

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%28255%2C68%2C68%2C1%29&t=one-dark&wt=none&l=text%2Fx-csharp&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=13px&lh=152%25&si=false&es=2x&wm=false&code=%252F%252F%2520Traditional%2520JWT%2520validation%2520-%2520100%252B%2520lines%2520per%2520issuer!%250Abuilder.Services.AddAuthentication%28JwtBearerDefaults.AuthenticationScheme%29%250A%2520%2520%2520%2520.AddJwtBearer%28options%2520%253D%253E%250A%2520%2520%2520%2520%257B%250A%2520%2520%2520%2520%2520%2520%2520%2520options.Authority%2520%253D%2520%2522https%253A%252F%252Ftenant.auth0.com%252F%2522%253B%250A%2520%2520%2520%2520%2520%2520%2520%2520options.Audience%2520%253D%2520%2522https%253A%252F%252Fmy-api%2522%253B%250A%2520%2520%2520%2520%2520%2520%2520%2520options.RequireHttpsMetadata%2520%253D%2520true%253B%250A%2520%2520%2520%2520%2520%2520%2520%2520options.TokenValidationParameters%2520%253D%2520new%2520TokenValidationParameters%250A%2520%2520%2520%2520%2520%2520%2520%2520%257B%250A%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520ValidateIssuerSigningKey%2520%253D%2520true%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520ValidateAudience%2520%253D%2520true%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520ValidateIssuer%2520%253D%2520true%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520ValidateLifetime%2520%253D%2520true%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520%2520ClockSkew%2520%253D%2520TimeSpan.FromMinutes%285%29%250A%2520%2520%2520%2520%2520%2520%2520%2520%257D%253B%250A%2520%2520%2520%2520%257D%29%253B%250A%252F%252F%2520%252B%2520JWKS%2520caching%252C%2520error%2520handling%252C%2520multi-issuer...%250A%252F%252F%2520%252B%2520200%2520more%2520lines%2520for%2520each%2520additional%2520provider!
```

**Or copy this code:**
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
// + JWKS caching, error handling, multi-issuer...
// + 200 more lines for each additional provider!
```
**Settings:** Background: #FF4444 (Red), Theme: One Dark

---

## SCENE 5: Identity — Primus Solution (GREEN background)
**Use for:** Showing the clean solution

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%280%2C200%2C83%2C1%29&t=dracula&wt=none&l=text%2Fx-csharp&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=14px&lh=152%25&si=false&es=2x&wm=false&code=%252F%252F%2520Program.cs%2520-%2520That%27s%2520it.%2520ONE%2520LINE!%250Abuilder.Services.AddPrimusIdentity%28options%2520%253D%253E%250A%257B%250A%2520%2520%2520%2520builder.Configuration.GetSection%28%2522PrimusIdentity%2522%29.Bind%28options%29%253B%250A%257D%29%253B%250A%250A%252F%252F%2520Middleware%2520-%2520Standard%2520ASP.NET%250Aapp.UseAuthentication%28%29%253B%250Aapp.UseAuthorization%28%29%253B%250A%250A%252F%252F%2520Optional%253A%2520Diagnostics%2520endpoint%250Aapp.MapPrimusIdentityDiagnostics%28%29%253B
```

**Or copy this code:**
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
**Settings:** Background: #00C853 (Green), Theme: Dracula

---

## SCENE 6: Identity — Configuration JSON
**Use for:** Multi-issuer config

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%2845%2C55%2C72%2C1%29&t=night-owl&wt=none&l=application%2Fjson&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=13px&lh=152%25&si=false&es=2x&wm=false&code=%257B%250A%2520%2520%2522PrimusIdentity%2522%253A%2520%257B%250A%2520%2520%2520%2520%2522Issuers%2522%253A%2520%255B%250A%2520%2520%2520%2520%2520%2520%257B%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Name%2522%253A%2520%2522AzureAD%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Type%2522%253A%2520%2522Oidc%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Authority%2522%253A%2520%2522https%253A%252F%252Flogin.microsoftonline.com%252F%257Btenant%257D%252Fv2.0%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Audiences%2522%253A%2520%255B%2522api%253A%252F%252Fyour-app-id%2522%255D%250A%2520%2520%2520%2520%2520%2520%257D%252C%250A%2520%2520%2520%2520%2520%2520%257B%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Name%2522%253A%2520%2522Auth0%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Type%2522%253A%2520%2522Oidc%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Authority%2522%253A%2520%2522https%253A%252F%252Fyour-tenant.auth0.com%252F%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Audiences%2522%253A%2520%255B%2522https%253A%252F%252Fyour-api%2522%255D%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522AllowMachineToMachine%2522%253A%2520true%250A%2520%2520%2520%2520%2520%2520%257D%252C%250A%2520%2520%2520%2520%2520%2520%257B%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Name%2522%253A%2520%2522LocalDev%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Type%2522%253A%2520%2522Jwt%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Issuer%2522%253A%2520%2522https%253A%252F%252Flocalhost%253A5001%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520%2522Secret%2522%253A%2520%2522your-dev-signing-key%2522%250A%2520%2520%2520%2520%2520%2520%257D%250A%2520%2520%2520%2520%255D%250A%2520%2520%257D%250A%257D
```

**Or copy this code:**
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
**Settings:** Background: #2D3748 (Dark blue-gray), Theme: Night Owl

---

## SCENE 8: Logging — Primus Solution (GREEN)
**Use for:** Clean logging setup

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%280%2C200%2C83%2C1%29&t=dracula&wt=none&l=text%2Fx-csharp&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=14px&lh=152%25&si=false&es=2x&wm=false&code=%252F%252F%2520Program.cs%2520-%2520Enterprise%2520logging%2520in%25203%2520lines%250Abuilder.Logging.ClearProviders%28%29%253B%250Abuilder.Logging.AddPrimus%28options%2520%253D%253E%250A%257B%250A%2520%2520%2520%2520builder.Configuration.GetSection%28%2522PrimusLogging%2522%29.Bind%28options%29%253B%250A%257D%29%253B%250A%250A%252F%252F%2520Add%2520middleware%2520for%2520HTTP%2520context%2520enrichment%250Aapp.UsePrimusLogging%28%29%253B
```

**Or copy this code:**
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
**Settings:** Background: #00C853 (Green), Theme: Dracula

---

## SCENE 9A: PII — Before Masking (RED)
**Use for:** Left side of comparison

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%28255%2C68%2C68%2C1%29&t=one-dark&wt=none&l=application%2Fjson&width=400&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=13px&lh=152%25&si=false&es=2x&wm=false&code=%257B%250A%2520%2520%2522message%2522%253A%2520%2522User%2520john.doe%2540company.com%2520logged%2520in%2522%252C%250A%2520%2520%2522context%2522%253A%2520%257B%250A%2520%2520%2520%2520%2522email%2522%253A%2520%2522john.doe%2540company.com%2522%252C%250A%2520%2520%2520%2520%2522creditCard%2522%253A%2520%25224532-1234-5678-9012%2522%252C%250A%2520%2520%2520%2520%2522ssn%2522%253A%2520%2522123-45-6789%2522%250A%2520%2520%257D%250A%257D
```

**Or copy this code:**
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
**Settings:** Background: #FF4444 (Red), Width: 400px

---

## SCENE 9B: PII — After Masking (GREEN)
**Use for:** Right side of comparison

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%280%2C200%2C83%2C1%29&t=dracula&wt=none&l=application%2Fjson&width=400&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=13px&lh=152%25&si=false&es=2x&wm=false&code=%257B%250A%2520%2520%2522message%2522%253A%2520%2522User%2520***REDACTED***%2520logged%2520in%2522%252C%250A%2520%2520%2522context%2522%253A%2520%257B%250A%2520%2520%2520%2520%2522email%2522%253A%2520%2522***REDACTED***%2522%252C%250A%2520%2520%2520%2520%2522creditCard%2522%253A%2520%2522***REDACTED***%2522%252C%250A%2520%2520%2520%2520%2522ssn%2522%253A%2520%2522***REDACTED***%2522%250A%2520%2520%257D%250A%257D
```

**Or copy this code:**
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
**Settings:** Background: #00C853 (Green), Width: 400px

---

## SCENE 10: Notifications Setup
**Use for:** Multi-channel notifications

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%28108%2C92%2C231%2C1%29&t=dracula&wt=none&l=text%2Fx-csharp&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=13px&lh=152%25&si=false&es=2x&wm=false&code=%252F%252F%2520Program.cs%2520-%2520Multi-channel%2520notifications%250Abuilder.Services.AddPrimusNotifications%28notifications%2520%253D%253E%250A%257B%250A%2520%2520%2520%2520%252F%252F%2520Email%2520via%2520SMTP%250A%2520%2520%2520%2520notifications.UseSmtp%28builder.Configuration.GetSection%28%2522Smtp%2522%29%29%253B%250A%2520%2520%2520%2520%250A%2520%2520%2520%2520%252F%252F%2520Templates%2520from%2520file%2520system%250A%2520%2520%2520%2520notifications.UseFileTemplates%28%2522NotificationTemplates%2522%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520validateOnStartup%253A%2520true%252C%250A%2520%2520%2520%2520%2520%2520%2520%2520watchForChanges%253A%2520true%29%253B%250A%2520%2520%2520%2520%250A%2520%2520%2520%2520%252F%252F%2520Built-in%2520queue%2520with%2520retry%2520logic%250A%2520%2520%2520%2520notifications.UseInMemoryQueue%28o%2520%253D%253E%250A%2520%2520%2520%2520%257B%250A%2520%2520%2520%2520%2520%2520%2520%2520o.BoundedCapacity%2520%253D%2520500%253B%250A%2520%2520%2520%2520%2520%2520%2520%2520o.MaxParallelHandlers%2520%253D%25202%253B%250A%2520%2520%2520%2520%257D%29%253B%250A%257D%29%253B
```

**Or copy this code:**
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
**Settings:** Background: #6C5CE7 (Purple), Theme: Dracula

---

## SCENE 11: Liquid Template
**Use for:** Template example

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%2845%2C55%2C72%2C1%29&t=night-owl&wt=none&l=text%2Fhtml&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=14px&lh=152%25&si=false&es=2x&wm=false&code=%257B%2525%2520include%2520%27Partials%252Femail_header%27%2520%2525%257D%250A%250AHi%2520%257B%257B%2520recipient.name%2520%257C%2520default%253A%2520%2522there%2522%2520%257D%257D%252C%250A%250AYour%2520password%2520reset%2520code%2520is%253A%2520%257B%257B%2520data.code%2520%257D%257D%250A%250AThis%2520code%2520expires%2520in%2520%257B%257B%2520data.expiryMinutes%2520%257D%257D%2520minutes.%250A%250A%257B%2525%2520include%2520%27Partials%252Femail_footer%27%2520%2525%257D
```

**Or copy this code:**
```liquid
{% include 'Partials/email_header' %}

Hi {{ recipient.name | default: "there" }},

Your password reset code is: {{ data.code }}

This code expires in {{ data.expiryMinutes }} minutes.

{% include 'Partials/email_footer' %}
```
**Settings:** Background: #2D3748, Theme: Night Owl

---

## SCENE 12: Feature Flags
**Use for:** Coming soon preview

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%28155%2C89%2C182%2C1%29&t=night-owl&wt=none&l=text%2Fx-csharp&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=13px&lh=152%25&si=false&es=2x&wm=false&code=%252F%252F%2520Feature%2520Flags%2520-%2520Controlled%2520rollouts%250Aif%2520%28featureFlags.IsEnabled%28%2522NewDashboard%2522%252C%2520userId%29%29%250A%257B%250A%2520%2520%2520%2520%252F%252F%2520Show%2520new%2520dashboard%2520to%2520selected%2520users%250A%257D%250A%250A%252F%252F%2520Percentage-based%2520rollout%250Avar%2520config%2520%253D%2520new%2520FeatureFlagConfig%250A%257B%250A%2520%2520%2520%2520Name%2520%253D%2520%2522NewCheckout%2522%252C%250A%2520%2520%2520%2520RolloutPercentage%2520%253D%252025%252C%2520%2520%252F%252F%252025%2525%2520of%2520users%250A%2520%2520%2520%2520TargetGroups%2520%253D%2520%255B%2522beta-testers%2522%252C%2520%2522premium%2522%255D%252C%250A%2520%2520%2520%2520TimeWindow%2520%253D%2520new%2520TimeWindow%28start%252C%2520end%29%250A%257D%253B
```

**Or copy this code:**
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
**Settings:** Background: #9B59B6 (Purple), Theme: Night Owl

---

## SCENE 14: Terminal Commands
**Use for:** Getting started

**Carbon Link:**
```
https://carbon.now.sh/?bg=rgba%2830%2C58%2C95%2C1%29&t=cobalt&wt=none&l=application%2Fx-sh&width=680&ds=false&dsyoff=20px&dsblur=68px&wc=true&wa=true&pv=24px&ph=32px&ln=false&fl=1&fm=Fira+Code&fs=14px&lh=152%25&si=false&es=2x&wm=false&code=%2523%2520Get%2520started%2520in%25205%2520minutes%250A%250A%2523%2520Step%25201%253A%2520Install%2520packages%250Adotnet%2520add%2520package%2520PrimusSaaS.Identity.Validator%250Adotnet%2520add%2520package%2520PrimusSaaS.Logging%250Adotnet%2520add%2520package%2520PrimusSaaS.Notifications%250A%250A%2523%2520Step%25202%253A%2520Run%2520your%2520app%250Adotnet%2520run%250A%250A%2523%2520Step%25203%253A%2520Test%2520authentication%250Acurl%2520-H%2520%2522Authorization%253A%2520Bearer%2520%253Ctoken%253E%2522%2520https%253A%252F%252Flocalhost%253A5001%252Fwhoami%250A%250A%2523%2520%25E2%259C%2585%2520You%27re%2520done!%2520Ship%2520features%252C%2520not%2520infrastructure.
```

**Or copy this code:**
```bash
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
**Settings:** Background: #1E3A5F (Navy), Theme: Cobalt

---

# 🎨 AI IMAGE PROMPTS (for non-code scenes)

## SCENE 1: Frustrated Developer
**Google Vids / Midjourney Prompt:**
```
Frustrated software developer at desk with multiple computer monitors showing complex code, clock on wall showing late hour, deadline calendar with red marks in background, modern office environment, cinematic lighting, professional photograph style, 16:9 aspect ratio
```

## SCENE 3: Security Shield
**Google Vids / Midjourney Prompt:**
```
Modern data security concept, glowing blue shield protecting data flow, GDPR and HIPAA compliance badge icons floating nearby, abstract tech background with circuit patterns, clean minimalist corporate style, blue and white color scheme, 16:9 aspect ratio
```

## SCENE 13: Time Savings Chart
**Google Vids / Midjourney Prompt:**
```
Professional business infographic showing dramatic time comparison chart, left bar showing "3 months" in red, right bar showing "1 day" in green, clean corporate design, blue and green accent colors, modern flat design style, 16:9 aspect ratio
```

## SCENE 15: Closing Logo
**Google Vids / Midjourney Prompt:**
```
Modern tech startup logo reveal, text "Primus SaaS" in bold clean font, rocket ship launching icon, gradient background transitioning from dark blue to purple, professional corporate branding style, 16:9 aspect ratio
```

---

# 📋 QUICK CHECKLIST

| Scene | Carbon Link | AI Image | Done? |
|-------|-------------|----------|-------|
| 1 | - | ✅ Frustrated dev | ☐ |
| 2 | ✅ Package install | - | ☐ |
| 3 | - | ✅ Security shield | ☐ |
| 4 | ✅ Traditional (RED) | - | ☐ |
| 5 | ✅ Primus (GREEN) | - | ☐ |
| 6 | ✅ JSON config | - | ☐ |
| 7 | Use manual code | - | ☐ |
| 8 | ✅ Logging (GREEN) | - | ☐ |
| 9 | ✅ PII before/after | - | ☐ |
| 10 | ✅ Notifications | - | ☐ |
| 11 | ✅ Liquid template | - | ☐ |
| 12 | ✅ Feature flags | - | ☐ |
| 13 | - | ✅ Time chart | ☐ |
| 14 | ✅ Terminal | - | ☐ |
| 15 | - | ✅ Logo | ☐ |

---

**Total Images Needed: 15**
- **11 from Carbon.now.sh** (code screenshots)
- **4 from AI generation** (scenes 1, 3, 13, 15)

---

*Ready for production!*
