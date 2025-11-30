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
        opts.ThrowOnFailure = false;
        opts.FallbackToLogger = true;
        opts.QueueOnFailure = true;
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

var app = builder.Build();

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
    var logsDir = Path.Combine(builder.Environment.ContentRootPath, "logs");
    if (!Directory.Exists(logsDir))
    {
        return Results.Json(new { message = "Log directory not found." });
    }

    // Prefer the dev log file name, fall back to any log in the directory.
    var candidates = new[]
    {
        Path.Combine(logsDir, "livedemo-api.dev.log"),
        Path.Combine(logsDir, "livedemo-api.log")
    }.Concat(Directory.GetFiles(logsDir, "*.log")).Distinct().ToList();

    var logFile = candidates.FirstOrDefault(File.Exists);
    if (logFile == null)
    {
        return Results.Json(new { message = "No log file found." });
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
            return Results.Ok(new
            {
                message = "Notification dispatched",
                channel = result.ChannelUsed,
                queued = result.EnqueuedForRetry
            });
        }

        logger.LogWarning("Notification failed: {Reason}", result.FailureReason);
        return Results.Problem(
            detail: result.FailureReason ?? "Failed to dispatch notification.",
            statusCode: result.ServiceUnavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status400BadRequest);
    }
    catch (NotificationFailedException ex)
    {
        logger.LogError(ex, "Notification threw");
        return Results.Problem(ex.Result.FailureReason ?? ex.Message);
    }
});

app.MapPost("/notifications/sms", async (SendSmsRequest request, INotificationService notifications, ILogger<Program> logger) =>
{
    try
    {
        var result = await notifications.SendSmsAsync(request.PhoneNumber, request.Message);

        if (result.Success)
        {
            return Results.Ok(new
            {
                message = "SMS dispatched",
                channel = result.ChannelUsed,
                queued = result.EnqueuedForRetry
            });
        }

        logger.LogWarning("SMS failed: {Reason}", result.FailureReason);
        return Results.Problem(
            detail: result.FailureReason ?? "Failed to dispatch SMS.",
            statusCode: result.ServiceUnavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status400BadRequest);
    }
    catch (NotificationFailedException ex)
    {
        logger.LogError(ex, "SMS notification threw");
        return Results.Problem(ex.Result.FailureReason ?? ex.Message);
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record SendWelcomeRequest(string Email, string Name);
record SendSmsRequest(string PhoneNumber, string Message);
