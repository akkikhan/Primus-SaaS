using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Notifications.Core;
using PrimusSaaS.Notifications.Services;
using PrimusSaaS.FeatureFlags;
using Primus.Documents;
using Primus.Documents.SelfTest;
using Primus.Documents.LinkStore;

var builder = WebApplication.CreateBuilder(args);
var templatesRoot = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");

// =============================================================
// 1) Logging (structured, PII redaction, optional AI/file sinks)
// =============================================================
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(opts => builder.Configuration.GetSection("PrimusLogging").Bind(opts));

var aiConnectionString = builder.Configuration["PrimusLogging:ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConnectionString) && aiConnectionString != "your-application-insights-connection-string")
{
    builder.Services.AddApplicationInsightsTelemetry(o => o.ConnectionString = aiConnectionString);
}

// Basic ASP.NET services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// =============================================================
// 2) Identity (multi-issuer validation + diagnostics)
// =============================================================
builder.Services.AddPrimusIdentity(opts => builder.Configuration.GetSection("PrimusIdentity").Bind(opts));
builder.Services.AddAuthorization();

// =============================================================
// 3) Notifications (templates + logger; SMTP/Twilio if configured)
// =============================================================
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications.UseFileTemplates(
        templatesRoot,
        validateOnStartup: true,
        watchForChanges: builder.Environment.IsDevelopment());

    notifications.UseLogger();
    notifications.UseInMemoryQueue(o =>
    {
        o.BoundedCapacity = 500;
        o.MaxParallelHandlers = 2;
        o.BaseRetryDelayMs = 250;
    });

    var smtpSection = builder.Configuration.GetSection("Notifications:Smtp");
    var smtpHost = smtpSection["Host"];
    var smtpFrom = smtpSection["FromAddress"];
    if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpFrom))
    {
        notifications.UseSmtp(opts =>
        {
            opts.Host = smtpHost!;
            opts.Port = smtpSection.GetValue("Port", 587);
            opts.Username = smtpSection["Username"] ?? string.Empty;
            opts.Password = smtpSection["Password"] ?? string.Empty;
            opts.EnableSsl = smtpSection.GetValue("EnableSsl", true);
            opts.FromAddress = smtpFrom!;
            opts.FromName = smtpSection["FromName"] ?? "Primus Live Demo";
            opts.MaxRetryCount = smtpSection.GetValue("MaxRetryCount", 2);
            opts.RetryBaseDelayMs = smtpSection.GetValue("RetryBaseDelayMs", 200);
        });
    }

    var twilioOptions = builder.Configuration.GetSection("Notifications:Twilio").Get<TwilioOptions>() ?? new TwilioOptions();
    if (twilioOptions.IsConfigured())
    {
        notifications.UseTwilio(builder.Configuration, "Notifications:Twilio", validateOnStartup: false);
    }
    else
    {
        notifications.UseSms(); // logger SMS for local/dev
    }

    notifications.ConfigureDispatch(o =>
    {
        o.ThrowOnFailure = true;
        o.FallbackToLogger = false;
        o.QueueOnFailure = false;
    });
});

// =============================================================
// 4) Feature Flags (in-memory provider; percentage/user targeting)
// =============================================================
builder.Services.AddPrimusFeatureFlags(opts => builder.Configuration.GetSection("PrimusFeatureFlags").Bind(opts));

// =============================================================
// 5) Document Renderer (text/markdown/html -> PDF + self-test)
// =============================================================
builder.Services.AddPrimusDocumentRenderer(opts => builder.Configuration.GetSection("PrimusDocuments").Bind(opts));

// Misc services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "https://localhost:5173",
                  "http://localhost:5174",
                  "https://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

var app = builder.Build();

// Swagger for demo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UsePrimusLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapPrimusIdentityDiagnostics(); // /primus/diagnostics

app.MapControllers();

app.Run();

public partial class Program { }
