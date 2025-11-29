using PrimusSaaS.Identity.Validator;
using PrimusSaaS.Logging;
using PrimusSaaS.Logging.Extensions;
using Primus.Notifications;  // NOTE: v1.2.0 on NuGet still uses OLD namespace!
using Primus.Notifications.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// ============================================
// BEGINNER ATTEMPT #2: Using correct API names from READMEs
// ============================================

// 1. Identity Validator - Correct: AddPrimusIdentity
builder.Services.AddPrimusIdentity(options =>
{
    options.UseAuth0("my-tenant.auth0.com", "https://my-api");
});
builder.Services.AddAuthorization();

// 2. Logging - Correct: builder.Logging.AddPrimus()
builder.Logging.ClearProviders();
builder.Logging.AddPrimus(options =>
{
    options.ApplicationId = "MyEcommerceApp";
});

// 3. Notifications - Using OLD namespace (Primus.Notifications) from v1.2.0
builder.Services.AddPrimusNotifications(notifications =>
{
    notifications
        .UseSmtp(opts =>
        {
            opts.Host = "smtp.example.com";
            opts.Port = 587;
            opts.Username = "user";
            opts.Password = "pass";
            opts.FromAddress = "noreply@myapp.com";
            opts.FromName = "My E-Commerce";
            opts.EnableSsl = true;
        })
        .UseFileTemplates(Path.Combine(builder.Environment.ContentRootPath, "Templates"))
        .UseInMemoryQueue()
        .UseLogger();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UsePrimusLogging();  // Enriches logs with HTTP context

app.MapControllers();

// Simple test endpoint
app.MapGet("/", () => "My E-Commerce API is running!");

// Test notification endpoint - using NotificationService from DI
app.MapGet("/test-notification", async (NotificationService notificationService) =>
{
    // Try to send a test email
    var result = await notificationService.SendEmailAsync(
        "test@example.com",
        "Test Subject",
        "<h1>Hello!</h1><p>This is a test email.</p>"
    );
    
    return result.Success ? "Email sent!" : $"Failed: {result.FailureReason}";
});

app.Run();
