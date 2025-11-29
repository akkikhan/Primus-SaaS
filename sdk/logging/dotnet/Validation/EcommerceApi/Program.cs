using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Notifications;

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
// Note: For real Auth0/Azure AD integration, configure proper options
// This demonstrates the API - in production, use real issuer values
builder.Services.AddPrimusIdentity(options =>
{
    // Custom issuer configuration for testing
    // In production, use .UseAuth0() or .UseAzureAd() helpers
    options.Issuers = new List<IssuerConfig>
    {
        new IssuerConfig
        {
            Name = "test-issuer",
            Issuer = "https://test.example.com/",
            Audiences = new List<string> { "ecommerce-api" },
            Type = IssuerType.Jwt,
            Secret = "test-secret-key-for-demo-only-replace-in-production"
        }
    };
});

// ============================================
// 3. PRIMUS NOTIFICATIONS CONFIGURATION
// ============================================
builder.Services.AddPrimusNotifications(notifications =>
{
    // File-based templates (required for SMTP/SMS channels)
    var templatesPath = Path.Combine(AppContext.BaseDirectory, "Templates");
    Directory.CreateDirectory(templatesPath);
    notifications.UseFileTemplates(templatesPath);
    
    // Configure Email (SMTP) - use fake SMTP for testing
    notifications.UseSmtp(smtp =>
    {
        smtp.Host = "localhost";
        smtp.Port = 1025; // MailHog/Papercut default
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
    ["packages"] = "PrimusSaaS.Logging 1.2.2, PrimusSaaS.Identity.Validator 1.3.3, PrimusSaaS.Notifications 1.1.0"
});

app.Run();

logger.Info("E-Commerce API shutting down");
