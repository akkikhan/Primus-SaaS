// Beginner Developer: Testing PrimusSaaS packages integration
using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Logging.Extensions;
using PrimusSaaS.Notifications;
using PrimusSaaS.Notifications.Abstractions;  // For INotificationService interface
using PrimusSaaS.Notifications.Core;           // For NotificationFailedException
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. LOGGING - Following the README
// ============================================
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MY-FIRST-PRIMUS-APP";
    options.Environment = "development";
});

// ============================================
// 2. IDENTITY - Following Auth0 Quick Start
// ============================================
builder.Services.AddPrimusIdentity(options =>
{
    // Using local JWT for testing (no external IdP)
    options.Issuers.Add(new IssuerConfig
    {
        Name = "LocalAuth",
        Type = IssuerType.Jwt,
        Issuer = "https://localhost",
        Secret = "my-super-secret-key-for-testing-1234567890",
        Audiences = new List<string> { "api://my-first-primus-app" }
    });
    
    options.RequireHttpsMetadata = false; // Allow HTTP in development
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ============================================
// 3. NOTIFICATIONS - Following Quick Start
// ============================================
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications
        .UseSmtp(opts =>
        {
            opts.Host = "smtp.example.com";
            opts.Port = 587;
            opts.Username = "test@example.com";
            opts.Password = "password123";
            opts.FromAddress = "no-reply@example.com";
            opts.FromName = "My App";
            opts.EnableSsl = true;
        })
        .UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "NotificationTemplates"))
        .UseInMemoryQueue()
        .UseLogger(); // Log notifications for testing
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Add Primus Logging middleware
app.UsePrimusLogging();

app.MapControllers();

// ============================================
// TEST ENDPOINTS
// ============================================

// Public endpoint
app.MapGet("/", () => "Hello from My First Primus App!");

// Protected endpoint (requires auth)
app.MapGet("/protected", [Authorize] (HttpContext ctx) =>
{
    var user = ctx.GetPrimusUser();
    return Results.Ok(new
    {
        message = "You are authenticated!",
        userId = user?.UserId,
        email = user?.Email
    });
});

// Test notification endpoint - using INotificationService interface (recommended)
app.MapPost("/send-test-email", async (INotificationService notificationService, ILogger<Program> logger) =>
{
    try
    {
        var result = await notificationService.SendEmailAsync(
            "test@example.com",
            "Test Subject",
            "<p>This is a test email from PrimusSaaS!</p>"
        );
        
        if (result.Success)
        {
            return Results.Ok(new { message = "Email sent successfully!" });
        }
        else
        {
            return Results.BadRequest(new { error = result.FailureReason });
        }
    }
    catch (NotificationFailedException ex)
    {
        logger.LogError(ex, "Notification failed");
        return Results.Problem("Failed to send notification");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error");
        return Results.Problem("Unexpected error");
    }
});

app.Run();
