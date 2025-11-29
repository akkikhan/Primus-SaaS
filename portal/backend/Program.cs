using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PrimusSaaS.Portal.Api.Data;
using PrimusSaaS.Portal.Api.Models;
using PrimusSaaS.Portal.Api.Services;
using System.Text;
using AspNetCoreRateLimit;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using System.Security.Claims;
using System.Threading;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, loggerConfiguration) =>
{
    var telemetryConnectionString = ctx.Configuration["Telemetry:ApplicationInsightsConnectionString"];

    loggerConfiguration
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
        .Enrich.WithProperty("Application", "PrimusPortal");

    if (!string.IsNullOrWhiteSpace(telemetryConnectionString))
    {
        loggerConfiguration.WriteTo.ApplicationInsights(
            new TelemetryConfiguration { ConnectionString = telemetryConnectionString },
            TelemetryConverter.Traces,
            restrictedToMinimumLevel: LogEventLevel.Information);
    }
});

// Add Application Insights telemetry (requests, dependencies, exceptions)
var aiConnectionString = builder.Configuration["Telemetry:ApplicationInsightsConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = aiConnectionString;
    });
}

// Database configuration (SQL Server by default, Postgres optional)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider")?.ToLowerInvariant();

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings__DefaultConnection is required for the portal backend.");
}

builder.Services.AddDbContext<PortalDbContext>(options =>
{
    switch (dbProvider)
    {
        case "sqlite":
            options.UseSqlite(connectionString);
            break;
        case "postgres":
        case "postgresql":
            options.UseNpgsql(connectionString);
            break;
        default:
            options.UseSqlServer(connectionString);
            break;
    }
});

// Register services
builder.Services.AddScoped<IWebhookSignatureValidator, WebhookSignatureValidator>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IModuleOwnershipService, ModuleOwnershipService>();

builder.Services.AddSingleton<LoggingMetrics>();

// Register Primus Notifications
builder.Services.AddPrimusNotifications(config =>
{
    var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
    if (emailSettings != null)
    {
        config.UseSmtp(options =>
        {
            options.Host = emailSettings.SmtpHost;
            options.Port = emailSettings.SmtpPort;
            options.Username = emailSettings.SmtpUser;
            options.Password = emailSettings.SmtpPass;
            options.EnableSsl = emailSettings.EnableSsl;
            options.FromAddress = emailSettings.FromAddress;
            options.FromName = "Primus SaaS";
        });
    }
    
    config.UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "Templates"));
    config.UseLogger();
    config.UseInMemoryQueue(options =>
    {
        options.BoundedCapacity = 1000;
        options.MaxParallelHandlers = 4;
        options.MaxRetryCount = 2;
        options.BaseRetryDelayMs = 200;
    });

    // Surface failures by default; allow disabling via configuration if callers prefer manual handling.
    config.ConfigureDispatch(opts =>
    {
        opts.ThrowOnFailure = builder.Configuration.GetValue("Notifications:ThrowOnFailure", true);
    });
});

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// JWT configuration (supports both Jwt and legacy JwtSettings sections)
var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration);
jwtOptions.Validate();
builder.Services.Configure<JwtOptions>(options =>
{
    options.Key = jwtOptions.Key;
    options.Secret = jwtOptions.Secret;
    options.Issuer = jwtOptions.Issuer;
    options.Audience = jwtOptions.Audience;
    options.ExpiryInMinutes = jwtOptions.ExpiryInMinutes;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.EffectiveKey!))
        };
    });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
                         new[] { "http://localhost:5173" };

    options.AddPolicy("FrontendOnly", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PortalDbContext>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var loggingMetrics = app.Services.GetRequiredService<LoggingMetrics>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Basic security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null || httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
        if (elapsed > 1000) return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };
});

app.Use(async (context, next) =>
{
    loggingMetrics.IncrementRequests();

    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
    var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value ?? "anonymous";
    var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "none";
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    using (LogContext.PushProperty("UserId", userId))
    using (LogContext.PushProperty("UserEmail", userEmail))
    using (LogContext.PushProperty("TenantId", tenantId))
    using (LogContext.PushProperty("ClientIp", clientIp))
    using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
    {
        try
        {
            await next();
        }
        catch
        {
            loggingMetrics.IncrementErrors();
            throw;
        }

        if (context.Response.StatusCode >= 500)
        {
            loggingMetrics.IncrementErrors();
        }
    }
});

app.UseIpRateLimiting();

app.UseCors("FrontendOnly");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/primus/logging/metrics", (LoggingMetrics metrics) =>
{
    var snapshot = metrics.Snapshot();
    return Results.Json(new
    {
        snapshot.Requests,
        snapshot.Errors
    });
}).RequireAuthorization();

// Initialize database and apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
    db.Database.Migrate();
    await EnsureSeedAdminAsync(scope.ServiceProvider, builder.Configuration);
}

app.Run();

static async Task EnsureSeedAdminAsync(IServiceProvider services, IConfiguration configuration)
{
    var email = configuration["SeedAdmin:Email"];
    var password = configuration["SeedAdmin:Password"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    var db = services.GetRequiredService<PortalDbContext>();

    if (await db.Users.AnyAsync(u => u.Email == email))
    {
        return;
    }

    var now = DateTime.UtcNow;
    var user = new User
    {
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Role = UserRole.Admin,
        CreatedAt = now,
        UpdatedAt = now
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();
}

public class LoggingMetrics
{
    private long _requests;
    private long _errors;

    public void IncrementRequests() => Interlocked.Increment(ref _requests);
    public void IncrementErrors() => Interlocked.Increment(ref _errors);
    public LoggingMetricsSnapshot Snapshot() =>
        new LoggingMetricsSnapshot(Interlocked.Read(ref _requests), Interlocked.Read(ref _errors));
}

public record LoggingMetricsSnapshot(long Requests, long Errors);
