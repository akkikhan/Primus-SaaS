# Primus Packages vs. Traditional DIY

Quick contrast of how you'd typically build things by hand versus using the Primus SDKs already in this repo.

## Notifications (`PrimusSaaS.Notifications`)

| Area | Traditional DIY | Using Primus |
| --- | --- | --- |
| Setup | Wire `SmtpClient`, Twilio SDK, retries, logging, and templating yourself; coordinate config in multiple spots. | `AddPrimusNotifications(...)` with SMTP/Twilio/logger options in one place. |
| Templates | Hardcoded strings or custom Razor/Liquid plumbing; manual reload on change. | File templates with startup validation and auto-reload in dev. |
| Delivery | Custom queue/retry/backoff logic per channel. | Built-in in-memory queue, bounded capacity, parallel handlers, configurable backoff/retries. |
| Multi-channel | Write branching logic for email/SMS/logger; duplicate templates. | Channels abstracted; one notification definition fans out to configured channels. |
| Observability | Hand-roll logging, correlation, and failure reporting. | Standardized logging channel plus dispatch result metadata. |

**Code comparison**

Traditional:
```csharp
// Program.cs
services.AddSingleton<SmtpClient>(_ =>
{
    var client = new SmtpClient("smtp.example.com", 587)
    {
        Credentials = new NetworkCredential(user, pass),
        EnableSsl = true
    };
    client.SendCompleted += OnSendCompleted; // your custom retry/backoff logic
    return client;
});
services.AddSingleton<TwilioRestClient>(_ => new TwilioRestClient(sid, token));
services.AddSingleton<INotificationQueue, InMemoryQueue>();
services.AddSingleton<INotificationService, CustomNotificationService>(); // templating + multi-channel + logging implemented by you
```

With Primus:
```csharp
builder.Services.AddPrimusNotifications(n =>
{
    n.UseFileTemplates("./Templates", validateOnStartup: true, watchForChanges: env.IsDevelopment());
    n.UseLogger();
    n.UseInMemoryQueue(o => { o.BoundedCapacity = 500; o.MaxParallelHandlers = 2; });
    n.UseSmtp(opts => { opts.Host = host; opts.Port = 587; opts.FromAddress = from; opts.EnableSsl = true; });
    n.UseTwilio(builder.Configuration, "Notifications:Twilio");
    n.ConfigureDispatch(o => { o.ThrowOnFailure = true; });
});
```

## Feature Flags (`PrimusSaaS.FeatureFlags`)

| Area | Traditional DIY | Using Primus |
| --- | --- | --- |
| Setup | Build your own flag store (JSON, DB), caching, and DI wiring. | `AddPrimusFeatureFlags(...)` with in-memory/JSON/Azure App Config providers out of the box. |
| Targeting | Write custom logic for users/groups/percentage rollouts. | Percentage rollouts via consistent hashing; users/groups/time windows built-in. |
| Hot reload | Manual file watching or cache invalidation. | JSON provider hot-reloads; providers swappable without app code changes. |
| Usage | `Configuration["FeatureX"]` checks scattered across code. | Typed service `IFeatureFlagService.IsEnabled("Flag", user)` with middleware support. |

**Code comparison**

Traditional:
```csharp
// Program.cs
var flags = JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText("featureflags.json"));
services.AddSingleton(flags);

// Usage
if (flags.TryGetValue("NewDashboard", out var enabled) && enabled)
{
    return ShowNewDashboard();
}
return ShowOldDashboard();
```

With Primus:
```csharp
builder.Services.AddPrimusFeatureFlags(options =>
{
    options.Provider = FeatureFlagProvider.JsonFile;
    options.JsonFilePath = "featureflags.json";
});

// Usage
if (_featureFlags.IsEnabled("NewDashboard", User))
{
    return ShowNewDashboard();
}
return ShowOldDashboard();
```

## Identity Validator (`PrimusSaaS.Identity.Validator`)

| Area | Traditional DIY | Using Primus |
| --- | --- | --- |
| Setup | Manually configure JWT bearer auth per issuer; handle key rotation and audiences. | `AddPrimusIdentity(...)` helpers for Auth0/Azure AD/Cognito/any issuer with sane defaults. |
| Multi-issuer | Custom scheme-per-issuer plus policy logic. | Multi-issuer list supported by one call; machine-to-machine toggle built-in. |
| Diagnostics | Trace 401s via logs and middleware debugging. | Diagnostics endpoint/header (`MapPrimusIdentityAuthDiagnostics`) gives failure reasons. |
| Maintenance | Keep up with OpenID metadata fetching, caching, and validation quirks. | Package handles metadata caching and validation options centrally. |

**Code comparison**

Traditional:
```csharp
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
            ValidateLifetime = true
        };
    });
```

With Primus:
```csharp
builder.Services.AddPrimusIdentity(options =>
{
    options.UseAuth0(
        domain: "tenant.auth0.com",
        audience: "https://my-api",
        allowMachineToMachine: false);
});
app.MapPrimusIdentityAuthDiagnostics();
```

## Documents (`PrimusSaaS.Documents`)

| Area | Traditional DIY | Using Primus |
| --- | --- | --- |
| Setup | Pick a PDF library (iText/QuestPDF), wrap it, and expose DI services. | `AddPrimusDocumentRenderer(builder.Configuration, "PrimusDocuments")` registers everything. |
| Inputs | Write converters for text/Markdown/HTML; handle unsafe content. | Supports text/Markdown/HTML with sane defaults; never logs document bodies. |
| Layout | Reimplement headers/footers, margins, branding per tenant. | Configurable margins, fonts, brand footer, timestamp/tenant flags. |
| Security/ops | Build your own link-based download tokens and self-tests. | Link TTLs and self-test modes built-in; tenant scoping enforced. |

**Code comparison**

Traditional:
```csharp
services.AddSingleton(new QuestPdfGenerator(/* custom options */));
services.AddScoped<IDocumentService, CustomDocumentService>(); // handles Markdown->HTML, PDF rendering, headers/footers, tokens
```

With Primus:
```csharp
builder.Services.AddPrimusDocumentRenderer(builder.Configuration, "PrimusDocuments");
// Later in controller:
var pdf = await _renderer.RenderPdfAsync(request);
return File(pdf, "application/pdf", $"{request.Title}.pdf");
```

## When to DIY vs. Use Primus

- Use Primus when you want standardized, configurable plumbing with retries/validation/logging handled for you.
- DIY only makes sense if you need highly custom behavior the packages don't support or you want to own every dependency surface.
