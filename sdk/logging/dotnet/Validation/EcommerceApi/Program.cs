using PrimusSaaS.Logging.Core;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Primus Logging
builder.Services.AddPrimusLogging(options =>
{
    options.ApplicationId = "ECOMMERCE-API";
    options.Environment = builder.Environment.EnvironmentName.ToLowerInvariant();
    options.MinLevel = builder.Environment.IsDevelopment() ? PrimusSaaS.Logging.Core.LogLevel.Debug : PrimusSaaS.Logging.Core.LogLevel.Info;

    options.Targets = new List<TargetConfig>
    {
        new() { Type = "console", Pretty = builder.Environment.IsDevelopment() },
        new() { Type = "file", Path = "logs/ecommerce.log" }
    };
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Simulate user authentication middleware (in real app, this would be Primus Identity Validator)
app.Use(async (context, next) =>
{
    // Simulate authenticated user
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
logger.Info("E-Commerce API starting", new Dictionary<string, object>
{
    ["environment"] = app.Environment.EnvironmentName,
    ["version"] = "1.0.0"
});

app.Run();

logger.Info("E-Commerce API shutting down");
