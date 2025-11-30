using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;
using PrimusSaaS.Notifications.Core;
using PrimusSaaS.Notifications.Services;
using PrimusSaaS.Notifications.Configuration;
using PrimusSaaS.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// Logging: structured logging with PII redaction + optional file sink
// =========================================================================
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    // Bind from configuration; safe defaults if section is missing
    builder.Configuration.GetSection("PrimusLogging").Bind(options);
});

// =========================================================================
// Application Insights: Full request/dependency telemetry + Primus traces
// =========================================================================
// Primus logging sends structured events as TraceTelemetry to AI.
// AddApplicationInsightsTelemetry adds Request/Dependency telemetry.
// Correlation IDs from Primus logging appear in trace properties.
var aiConnectionString = builder.Configuration["PrimusLogging:ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(aiConnectionString) && aiConnectionString != "your-application-insights-connection-string")
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = aiConnectionString;
    });
}

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =========================================================================
// DEMO STEP 1: Register Primus Identity Services
// =========================================================================
// This single line binds the configuration from appsettings.json
// and sets up all necessary validation logic for multiple providers.
builder.Services.AddPrimusIdentity(options =>
{
    builder.Configuration.GetSection("PrimusIdentity").Bind(options);
});

// =========================================================================
// DEMO STEP 1B: Register Primus Notifications Services
// =========================================================================
// Uses file-based templates and a logger channel by default.
// SMTP turns on only when real credentials are provided via configuration.
builder.Services.AddPrimusNotifications(notifications =>
{
    var templatesPath = Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates");
    notifications.UseFileTemplates(
        templatesPath,
        validateOnStartup: true,
        watchForChanges: builder.Environment.IsDevelopment());

    // Always include a logger sink so demos run without external providers.
    notifications.UseLogger();

    // Optional async queue to mimic production dispatch behavior.
    notifications.UseInMemoryQueue(options =>
    {
        options.BoundedCapacity = 500;
        options.MaxParallelHandlers = 2;
        options.BaseRetryDelayMs = 250;
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

    // SMS: use Twilio when configured, otherwise fall back to logger SMS channel.
    var twilioSection = builder.Configuration.GetSection("Notifications:Twilio");
    var twilioOptions = twilioSection.Get<TwilioOptions>() ?? new TwilioOptions();
    if (twilioOptions.IsConfigured())
    {
        notifications.UseTwilio(builder.Configuration, "Notifications:Twilio", validateOnStartup: false);
    }
    else
    {
        notifications.UseSms(); // logging sender for local/dev if no Twilio creds
    }

    notifications.ConfigureDispatch(opts =>
    {
        // Surface failures so Twilio issues are visible; avoid silent logger fallback.
        opts.ThrowOnFailure = true;
        opts.FallbackToLogger = false;
        opts.QueueOnFailure = false;
    });
});



// Standard ASP.NET Core Authorization
builder.Services.AddAuthorization();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5173",
                    "https://localhost:5173") // Vite dev server (http/https)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add HttpClient for Auth0 proxy
builder.Services.AddHttpClient();

// Enable PII for debugging
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

// Demo log path (for /logs/recent and UI viewer)
var demoLogDir = Path.Combine(builder.Environment.ContentRootPath, "logs");
Directory.CreateDirectory(demoLogDir);
var demoLogPath = Path.Combine(demoLogDir, "livedemo-api.log");

void WriteDemoLog(string message)
{
    try
    {
        File.AppendAllText(demoLogPath, $"[{DateTimeOffset.UtcNow:u}] {message}{Environment.NewLine}");
    }
    catch
    {
        // ignore for demo
    }
}

// Helper to log JSON payloads for demo visibility (writes to ILogger + demo log file)
void LogJson(ILogger logger, string message, object data)
{
    try
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        logger.LogInformation("{Message}: {Payload}", message, json);
        WriteDemoLog($"{message}: {json}");
    }
    catch
    {
        logger.LogInformation("{Message}: (unserializable payload)", message);
        WriteDemoLog($"{message}: (unserializable payload)");
    }
}

var app = builder.Build();

// Warm up logging so a file is created early for the demo log viewer
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Primus LiveDemo starting at {Time} (Environment: {Env})",
    DateTimeOffset.UtcNow,
    app.Environment.EnvironmentName);
WriteDemoLog($"Primus LiveDemo starting (Env: {app.Environment.EnvironmentName})");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// Structured request logging (with correlation IDs and PII redaction by default)
app.UsePrimusLogging();

// =========================================================================
// DEMO STEP 2: Add Middleware
// =========================================================================
// Ensure these are placed between UseHttpsRedirection and MapControllers
app.UseAuthentication();
app.UseAuthorization();

// =========================================================================
// DEMO STEP 3: Diagnostics (Optional)
// =========================================================================
// This endpoint (/primus/diagnostics) allows us to verify our configuration
// and see exactly which keys are loaded from Azure AD and Auth0.
app.MapPrimusIdentityDiagnostics();

// =========================================================================
// DEMO HELPER: Log viewer (for demo only — consider securing/removing for prod)
// =========================================================================
app.MapGet("/logs/recent", () =>
{
    var logDirs = new[]
    {
        Path.Combine(builder.Environment.ContentRootPath, "logs"),
        Path.Combine(AppContext.BaseDirectory, "logs")
    };
    foreach (var dir in logDirs)
    {
        Directory.CreateDirectory(dir);
    }

    // Prefer the dev log file name, fall back to any log in the directory.
    var candidates = logDirs
        .SelectMany(dir => new[]
        {
            Path.Combine(dir, "livedemo-api.dev.log"),
            Path.Combine(dir, "livedemo-api.log")
        }.Concat(Directory.GetFiles(dir, "*.log")))
        .Distinct()
        .ToList();

    var logFile = candidates.FirstOrDefault(File.Exists);
    if (logFile == null)
    {
        // Create a default file with a starter entry
        var defaultFile = Path.Combine(logDirs.First(), "livedemo-api.log");
        File.AppendAllText(defaultFile, $"[{DateTimeOffset.UtcNow:u}] Warmup log created by /logs/recent endpoint.{Environment.NewLine}");
        logFile = defaultFile;
    }

    const int maxBytes = 32 * 1024; // tail ~32KB
    var info = new FileInfo(logFile);
    var start = Math.Max(0, info.Length - maxBytes);
    using var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    stream.Seek(start, SeekOrigin.Begin);
    using var reader = new StreamReader(stream);
    var content = reader.ReadToEnd();

    return Results.Json(new
    {
        file = Path.GetFileName(logFile),
        size = info.Length,
        tail = content
    });
}).WithName("GetRecentLogs");

// =========================================================================
// DEMO HELPER: Real Token Proxy
// =========================================================================
// 1. Auth0 Proxy (Client Credentials Flow)
app.MapPost("/auth/auth0", async (IHttpClientFactory httpClientFactory, ILogger<Program> logger) =>
{
    logger.LogInformation("=== AUTH0 TOKEN REQUEST STARTED ===");
    var client = httpClientFactory.CreateClient();
    
    var requestBody = new
    {
        client_id = "h4CjtEYT0HiXwJVr3JkOSkJnr1aq3bHc",
        client_secret = "6Si0dfpi89xei4GGGcxblXIb2dc6r8RpfLqPAqaleN_sy3c6PmSLbrTfDAfm_sLm",
        audience = "https://saas-api/",
        grant_type = "client_credentials"
    };
    
    logger.LogInformation("Request Body: ClientId={ClientId}, Audience={Audience}", 
        requestBody.client_id, requestBody.audience);
    
    var response = await client.PostAsJsonAsync("https://dev-ft7bykiq2exe4ua4.us.auth0.com/oauth/token", requestBody);
    
    logger.LogInformation("Auth0 Response Status: {StatusCode}", response.StatusCode);
    
    var content = await response.Content.ReadAsStringAsync();
    
    if (response.IsSuccessStatusCode)
    {
        logger.LogInformation("✅ Token received successfully from Auth0");
        logger.LogInformation("Token preview: {TokenPreview}...", content.Substring(0, Math.Min(100, content.Length)));
    }
    else
    {
        logger.LogError("❌ Auth0 token request failed: {Content}", content);
    }
    
    LogJson(logger, "Auth0 token response", new { Status = response.StatusCode, ContentLength = content.Length });
    
    return Results.Content(content, "application/json");
});

// 2. Azure Proxy (CLI Token)
app.MapPost("/auth/azure", async () =>
{
    try
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c az account get-access-token --resource https://management.azure.com/ --query accessToken -o tsv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var token = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(token))
            return Results.BadRequest(new { error = $"Azure CLI Error: {error}. Ensure you are logged in with 'az login'." });

        return Results.Ok(new { access_token = token.Trim(), token_type = "Bearer" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// 3. Local JWT (shared secret for demo/local development)
app.MapPost("/auth/local", (LocalLoginRequest request, IConfiguration config, ILogger<Program> logger) =>
{
    var primusOptions = config.GetSection("PrimusIdentity").Get<PrimusIdentityOptions>() ?? new PrimusIdentityOptions();
    var localIssuer = primusOptions.Issuers.FirstOrDefault(i =>
        string.Equals(i.Name, "LocalJwt", StringComparison.OrdinalIgnoreCase));

    if (localIssuer == null)
    {
        return Results.BadRequest(new { error = "LocalJwt issuer not configured. Add PrimusIdentity:Issuers entry named 'LocalJwt'." });
    }

    if (string.IsNullOrWhiteSpace(localIssuer.Secret))
    {
        return Results.BadRequest(new { error = "LocalJwt secret is missing. Set PrimusIdentity:Issuers:LocalJwt:Secret." });
    }

    var demoAuth = config.GetSection("DemoLocalAuth");
    var expectedEmail = demoAuth["Email"] ?? "demo@primus.local";
    var expectedPassword = demoAuth["Password"] ?? "PrimusDemo123!";
    var displayName = demoAuth["Name"] ?? "Local Demo User";
    var subject = demoAuth["Subject"] ?? "local-demo-user";

    if (!string.Equals(request.Email, expectedEmail, StringComparison.OrdinalIgnoreCase) ||
        request.Password != expectedPassword)
    {
        logger.LogWarning("Local JWT login failed for {Email}", request.Email);
        return Results.BadRequest(new { error = "Invalid email or password for Local JWT demo user." });
    }

    var audience = localIssuer.Audiences.FirstOrDefault() ?? "api://primus-livedemo";
    var token = TestTokenBuilder.Create()
        .WithIssuer(localIssuer.Issuer)
        .WithAudience(audience)
        .WithSecret(localIssuer.Secret)
        .WithClaim("sub", subject)
        .WithClaim("email", request.Email)
        .WithClaim("name", displayName)
        .Build();

    logger.LogInformation("✅ Issued Local JWT for {Email} (issuer: {Issuer}, audience: {Audience})", request.Email, localIssuer.Issuer, audience);
    return Results.Ok(new { access_token = token, token_type = "Bearer", provider = localIssuer.Name });
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (HttpContext context, ILogger<Program> logger) =>
{
    logger.LogInformation("=== WEATHERFORECAST REQUEST ===");
    logger.LogInformation("User authenticated: {IsAuthenticated}", context.User.Identity?.IsAuthenticated);
    
    if (context.User.Identity?.IsAuthenticated == true)
    {
        logger.LogInformation("User claims:");
        foreach (var claim in context.User.Claims)
        {
            logger.LogInformation("  {Type}: {Value}", claim.Type, claim.Value);
        }
    }
    else
    {
        logger.LogWarning("❌ User is NOT authenticated");
    }
    
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi()
.RequireAuthorization(); // <--- DEMO STEP 4: Secure the endpoint

// =========================================================================
// DEMO: Notifications playground endpoints
// =========================================================================
app.MapGet("/notifications/health", async (NotificationHealthService health) =>
{
    var snapshot = await health.GetChannelHealthAsync();
    return Results.Json(snapshot);
});

app.MapPost("/notifications/welcome", async (SendWelcomeRequest request, INotificationService notifications, ILogger<Program> logger) =>
{
    LogJson(logger, "Notifications - welcome request", request);
    var notification = new BasicNotification(
        type: "Welcome",
        data: new { request.Name },
        recipient: new Recipient { Email = request.Email, Name = request.Name },
        channels: new[] { "Email", "Logger" });

    try
    {
        var result = await notifications.SendAsync(notification);

        if (result.Success)
        {
            LogJson(logger, "Notifications - welcome sent", result);
            return Results.Ok(new
            {
                message = "Notification dispatched",
                channel = result.ChannelUsed,
                queued = result.EnqueuedForRetry
            });
        }

        logger.LogWarning("Notification failed: {Reason}", result.FailureReason);
        LogJson(logger, "Notifications - welcome failed", result);
        return Results.Problem(
            detail: result.FailureReason ?? "Failed to dispatch notification.",
            statusCode: result.ServiceUnavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status400BadRequest);
    }
    catch (NotificationFailedException ex)
    {
        logger.LogError(ex, "Notification threw");
        LogJson(logger, "Notifications - welcome threw", new { ex.Result.FailureReason, ex.Message });
        return Results.Problem(ex.Result.FailureReason ?? ex.Message);
    }
});

app.MapPost("/notifications/sms", async (SendSmsRequest request, INotificationService notifications, ILogger<Program> logger) =>
{
    LogJson(logger, "Notifications - sms request", request);
    try
    {
        var result = await notifications.SendSmsAsync(request.PhoneNumber, request.Message);

        if (result.Success)
        {
            LogJson(logger, "Notifications - sms sent", result);
            return Results.Ok(new
            {
                message = "SMS dispatched",
                channel = result.ChannelUsed,
                queued = result.EnqueuedForRetry
            });
        }

        logger.LogWarning("SMS failed: {Reason}", result.FailureReason);
        LogJson(logger, "Notifications - sms failed", result);
        return Results.Problem(
            detail: result.FailureReason ?? "Failed to dispatch SMS.",
            statusCode: result.ServiceUnavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status400BadRequest);
    }
    catch (NotificationFailedException ex)
    {
        var detail = ex.Result.FailureReason ?? ex.Message;
        var channels = ex.Result.Channels.Select(c => new { c.Channel, c.Status, c.Detail }).ToArray();
        logger.LogError(ex, "SMS notification threw: {Detail}", detail);
        LogJson(logger, "Notifications - sms threw", new { detail, channels });
        return Results.Problem(detail: detail, statusCode: StatusCodes.Status502BadGateway, extensions: new Dictionary<string, object?>
        {
            ["channels"] = channels
        });
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record LocalLoginRequest(string Email, string Password);
record SendWelcomeRequest(string Email, string Name);
record SendSmsRequest(string PhoneNumber, string Message);
