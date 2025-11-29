using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================
// 1. PRIMUS LOGGING CONFIGURATION
// ============================================
builder.Services.AddPrimusLogging(options =>
{
    options.ApplicationId = "ECOMMERCE-API";
    options.Environment = builder.Environment.EnvironmentName.ToLowerInvariant();
    options.MinLevel = builder.Environment.IsDevelopment() 
        ? PrimusSaaS.Logging.Core.LogLevel.Debug 
        : PrimusSaaS.Logging.Core.LogLevel.Info;

    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = builder.Environment.IsDevelopment() },
        new() { Type = "file", Path = "logs/ecommerce.log" }
    };
});

// ============================================
// 2. PRIMUS IDENTITY VALIDATOR CONFIGURATION
// ============================================
// Multi-provider setup: Auth0 + Azure AD
builder.Services.AddPrimusIdentity(options =>
{
    options.Issuers = new List<IssuerConfig>
    {
        // Auth0 M2M token validation
        new IssuerConfig
        {
            Name = "auth0",
            Issuer = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/",
            Authority = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/",
            Audiences = new List<string> { "https://saas-api/" },
            Type = IssuerType.Auth0,
            JwksUrl = "https://dev-ft7bykiq2exe4ua4.us.auth0.com/.well-known/jwks.json",
            AllowMachineToMachine = true,
            AllowedGrantTypes = new List<string> { "client-credentials" }
        },
        // Azure AD / Entra ID M2M token validation
        new IssuerConfig
        {
            Name = "azuread",
            // Azure AD v1 issuer format (used by client credentials tokens)
            Issuer = "https://sts.windows.net/cbd15a9b-cd52-4ccc-916a-00e2edb13043/",
            Authority = "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043",
            // Audience is the API App ID with api:// prefix
            Audiences = new List<string> { "api://d91ce212-625e-4bdb-9b3f-428a831077a4" },
            Type = IssuerType.AzureAD,
            JwksUrl = "https://login.microsoftonline.com/cbd15a9b-cd52-4ccc-916a-00e2edb13043/discovery/keys",
            AllowMachineToMachine = true,
            AllowedGrantTypes = new List<string> { "client-credentials" }
        }
    };
    
    // Enable verbose logging for debugging
    options.Logging.LogValidationSteps = true;
    options.Logging.MinimumLevel = Microsoft.Extensions.Logging.LogLevel.Debug;
});

// ============================================
// 3. PRIMUS NOTIFICATIONS CONFIGURATION
// ============================================
builder.Services.AddPrimusNotifications(notifications =>
{
    // File-based templates (required for SMTP/SMS channels)
    var templatesPath = Path.Combine(AppContext.BaseDirectory, "Templates");
    Directory.CreateDirectory(templatesPath);
    notifications.UseFileTemplates(templatesPath, validateOnStartup: true);
    
    // Configure Email (SMTP) - use fake SMTP for testing
    notifications.UseSmtp(smtp =>
    {
        smtp.Host = "localhost";
        smtp.Port = 25; // Papercut on user's machine
        smtp.EnableSsl = false;
        smtp.FromAddress = "noreply@ecommerce-api.local";
        smtp.FromName = "E-Commerce API";
    });

    // Configure SMS (Twilio) - credentials loaded from configuration or environment
    notifications.UseTwilio(twilio =>
    {
        twilio.AccountSid = builder.Configuration["Twilio:AccountSid"] ?? Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID") ?? "";
        twilio.AuthToken = builder.Configuration["Twilio:AuthToken"] ?? Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? "";
        twilio.FromNumber = builder.Configuration["Twilio:FromNumber"] ?? Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER") ?? "";
    });

    // Prefer throwing on failures by default; can disable via configuration for softer handling.
    notifications.ConfigureDispatch(opts =>
    {
        opts.ThrowOnFailure = builder.Configuration.GetValue("Notifications:ThrowOnFailure", true);
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Simulate user authentication middleware (in real app, use Primus Identity Validator middleware)
app.Use(async (context, next) =>
{
    // Simulate authenticated user for demo purposes
    context.Items["PrimusUser"] = new Dictionary<string, object>
    {
        ["userId"] = "user-12345",
        ["email"] = "john.doe@acmecorp.com",
        ["name"] = "John Doe"
    };

    context.Items["PrimusTenantContext"] = new Dictionary<string, object>
    {
        ["tenantId"] = "tenant-acme",
        ["tenantName"] = "Acme Corporation"
    };

    await next();
});

// Use Primus Logging Middleware
app.UsePrimusLogging();

app.UseHttpsRedirection();

// CRITICAL: Must call UseAuthentication() before UseAuthorization()
// This enables the JWT Bearer middleware that validates Auth0 tokens
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Diagnostics endpoint for logging health (optional; protect in production)
app.MapPrimusLoggingHealth("/_primus/logging/health");

// Get logger and log startup
var logger = app.Services.GetRequiredService<Logger>();
logger.Info("E-Commerce API starting", new Dictionary<string, object?>
{
    ["environment"] = app.Environment.EnvironmentName,
    ["version"] = "2.0.0",
    ["packages"] = "PrimusSaaS.Logging 1.2.2, PrimusSaaS.Identity.Validator 1.3.4, PrimusSaaS.Notifications 1.3.1"
});

app.Run();

logger.Info("E-Commerce API shutting down");
